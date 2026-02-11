using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StoreItApi.Models
{
    [Table("Users")]
    public class User
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty; // เก็บแบบ Hash

        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = "STAFF"; // ADMIN, IT, STAFF
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}