using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace KSS.Entity
{
    /// <summary>
    /// Role catalog row. Read-only from the application layer — the catalog is
    /// maintained directly in the database via versioned migrations. No audit
    /// columns since end users / services never modify this table.
    /// </summary>
    [Table("Role", Schema = "dbo")]
    public class Role
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid Id { get; set; }

        // Machine-readable role identifier (e.g. "SuperAdmin", "PersonAdmin").
        // Used in JWT claims and code-level role checks.
        [Required]
        [MaxLength(50)]
        [Unicode(false)]
        public string Code { get; set; } = string.Empty;

        // FK by value (cross-DB) to KSS_Common.dbo.Module(Id). NULL = global role.
        public Guid? ModuleId { get; set; }

        public ICollection<RoleTranslation> Translations { get; set; } = new List<RoleTranslation>();
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}
