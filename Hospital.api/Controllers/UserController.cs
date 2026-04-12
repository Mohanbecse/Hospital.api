using Hospital.api.Models;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly HospitalDbContext _context;

        public UserController(HospitalDbContext context)
        {
            _context = context;
        }

        // 🔹 GET ALL USERS
        [HttpGet("Getuser")]
        public IActionResult GetUsers()
        {
            var userslist = _context.Users.ToList();
            return Ok(userslist);
        }

        // 🔹 ADD USER
        [HttpPost("Adduser")]
        public IActionResult PostUser([FromBody] User user)
        {
            if (string.IsNullOrEmpty(user.Username) || string.IsNullOrEmpty(user.Password))
            {
                return BadRequest("Username and Password are required");
            }

            var existingUser = _context.Users.Find(user.Username);
            if (existingUser != null)
            {
                return Conflict("Username already exists");
            }

            _context.Users.Add(user);
            _context.SaveChanges();

            return Ok("User added successfully");
        }

        // 🔹 UPDATE USER
        [HttpPut("Updateuser")]
        public IActionResult PutUser([FromBody] User user)
        {
            var existinguser = _context.Users.Find(user.Username);

            if (existinguser == null)
            {
                return NotFound("User not found");
            }

            existinguser.Password = user.Password;

            _context.SaveChanges();

            return Ok("User updated successfully");
        }

        // 🔹 DELETE USER
        [HttpDelete("Deleteuser/{username}")]
        public IActionResult DeleteUser(string username)
        {
            var existingUser = _context.Users.Find(username);

            if (existingUser == null)
            {
                return NotFound("User not found");
            }

            _context.Users.Remove(existingUser);
            _context.SaveChanges();

            return Ok("User deleted successfully");
        }

        // 🔹 LOGIN API
        [HttpPost("Login")]
        public IActionResult Login([FromBody] User login)
        {
            if (string.IsNullOrEmpty(login.Username) || string.IsNullOrEmpty(login.Password))
            {
                return BadRequest("Username and Password are required");
            }

            var user = _context.Users
                               .FirstOrDefault(u => u.Username == login.Username);

            if (user == null || user.Password != login.Password)
            {
                return Unauthorized("Invalid username or password");
            }

            return Ok(new
            {
                message = "Login successful",
                username = user.Username
            });
        }
    }
}