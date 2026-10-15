using System.ComponentModel.DataAnnotations.Schema;

namespace KSS.Entity
{
    [Table("RolePermission", Schema = "dbo")]
    public class RolePermission
    {
        public Guid RoleId { get; set; }
        public Guid PermissionId { get; set; }

        public Guid CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Guid? DeletedBy { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Navigation
        [ForeignKey("RoleId")]
        public Role Role { get; set; } = null!;

        [ForeignKey("PermissionId")]
        public Permission Permission { get; set; } = null!;
    }
}
