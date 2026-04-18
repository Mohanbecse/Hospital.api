using Hospital.api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Diagnostics;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// ==============================================
// PRODUCTION READY CONFIGURATION
// ==============================================

// Configure Logging for Production
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddEventSourceLogger();
if (builder.Environment.IsProduction())
{
    builder.Logging.SetMinimumLevel(LogLevel.Warning);
}

// Add Response Compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<GzipCompressionProvider>();
    options.Providers.Add<BrotliCompressionProvider>();
});

// Configure HSTS for Production
builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365);
});

// Add Health Checks
builder.Services.AddHealthChecks();

// Add services
builder.Services.AddControllers(options =>
{
    // Global Authorization Policy - requires authentication for ALL controllers by default
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.Authorization.AuthorizeFilter());

    // Input Validation
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = false;
})
.AddJsonOptions(options =>
{
    // Prevent circular references
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.WriteIndented = false;
});

// Add API Documentation with Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "Hospital API", Version = "v1" });
    
    // Add JWT Authentication to Swagger
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter 'Bearer {token}' in the field below"
    });
    
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// JWT Authentication Configuration
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
            ClockSkew = TimeSpan.Zero,
            RequireExpirationTime = true,
            RequireSignedTokens = true
        };

        if (!builder.Environment.IsDevelopment())
        {
            options.SaveToken = true;
            options.MapInboundClaims = true;
        }

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                context.Response.ContentType = "application/json";
                return context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    error = "Authentication failed",
                    message = "Invalid or expired token"
                }));
            }
        };
    });

builder.Services.AddAuthorization();

// Runtime configurable CORS policy
builder.Services.AddCors(opt =>
    opt.AddPolicy("AllowConfiguredOrigin", policy =>
    {
        var allowedOrigin = builder.Configuration["AllowedFrontendOrigin"];
        
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
        
        if (!string.IsNullOrEmpty(allowedOrigin))
        {
            policy.WithOrigins(allowedOrigin.Split(',', StringSplitOptions.RemoveEmptyEntries));
        }
        else if (builder.Environment.IsDevelopment())
        {
            policy.WithOrigins("http://localhost:4200", "http://localhost:3000", "http://localhost:5173", "http://localhost:8080");
        }
        else
        {
            throw new InvalidOperationException("AllowedFrontendOrigin must be configured in production environment");
        }
    })
);

// Database Context Configuration
builder.Services.AddDbContext<HospitalDbContext>(opt =>
{
    opt.UseSqlServer(builder.Configuration.GetConnectionString("con"), sqlOptions =>
    {
        sqlOptions.CommandTimeout(30);
        sqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
    });
    
    if (builder.Environment.IsDevelopment())
    {
        opt.EnableSensitiveDataLogging();
        opt.EnableDetailedErrors();
    }
});

var app = builder.Build();

// ==============================================
// MIDDLEWARE PIPELINE (ORDER MATTERS!)
// ==============================================

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Global Exception Handler
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        context.Response.ContentType = "application/json";

        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        
        var result = new
        {
            error = "An error occurred while processing your request",
            traceId = context.TraceIdentifier,
            message = app.Environment.IsDevelopment() ? exception?.Message : "Internal Server Error"
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(result));
    });
});

app.UseResponseCompression();

if (app.Environment.IsProduction())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

// Security Headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    
    if (app.Environment.IsProduction())
    {
        context.Response.Headers.Remove("X-Powered-By");
        context.Response.Headers.Remove("Server");
    }
    
    await next();
});

// Swagger Documentation - Enabled for ALL environments
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Hospital API v1");
    options.RoutePrefix = "swagger";
    options.DocumentTitle = "Hospital API Documentation";
    options.EnablePersistAuthorization();
    options.DisplayRequestDuration();
});

app.UseCors("AllowConfiguredOrigin");
app.UseAuthentication();
app.UseAuthorization();

// Health Check Endpoint
app.MapHealthChecks("/health").AllowAnonymous();

app.MapControllers();

// Run Database Migrations on Startup (Production Safe)
if (builder.Configuration.GetValue<bool>("RunMigrationsOnStartup") == true)
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();
        if (dbContext.Database.GetPendingMigrations().Any())
        {
            await dbContext.Database.MigrateAsync();
        }
    }
}

app.Run();
