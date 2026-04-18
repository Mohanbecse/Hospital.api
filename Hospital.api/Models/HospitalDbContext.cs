using Microsoft.EntityFrameworkCore;

namespace Hospital.api.Models
{
    public class HospitalDbContext:DbContext
    {
        public HospitalDbContext(DbContextOptions<HospitalDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Patient> Patients { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Patient>()
                .ToTable(tb => tb.HasTrigger("trg_Patients_Delete"));
        }
    }
}
