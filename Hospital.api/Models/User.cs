using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hospital.api.Models
{
    [Table("Users")]
    public class User
    {
        [Key]
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
