using System.ComponentModel.DataAnnotations.Schema;

namespace KSS.Entity
{
    [Table("UserRole", Schema = "dbo")]
    public class UserRole
    {
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }

        public DateTime AssignedAt { get; set; }

        // Navigation
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        [ForeignKey("RoleId")]
        public Role Role { get; set; } = null!;
    }
}
