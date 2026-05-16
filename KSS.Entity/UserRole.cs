using System.ComponentModel.DataAnnotations.Schema;

namespace KSS.Entity
{
    [Table("UserRole", Schema = "dbo")]
    public class UserRole
    {
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }

        public Guid CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Guid? DeletedBy { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Navigation
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        [ForeignKey("RoleId")]
        public Role Role { get; set; } = null!;
    }
}
