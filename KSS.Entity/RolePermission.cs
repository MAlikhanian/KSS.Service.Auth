using System.ComponentModel.DataAnnotations.Schema;

namespace KSS.Entity
{
    [Table("RolePermission", Schema = "dbo")]
    public class RolePermission
    {
        public Guid RoleId { get; set; }
        public Guid PermissionId { get; set; }

        public DateTime AssignedAt { get; set; }

        // Navigation
        [ForeignKey("RoleId")]
        public Role Role { get; set; } = null!;

        [ForeignKey("PermissionId")]
        public Permission Permission { get; set; } = null!;
    }
}
