using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace KSS.Entity
{
    /// <summary>
    /// Permission catalog row. Read-only from the application layer — the catalog
    /// is maintained directly in the database via versioned migrations. No audit
    /// columns since end users / services never modify this table.
    /// </summary>
    [Table("Permission", Schema = "dbo")]
    public class Permission
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid Id { get; set; }

        // Machine-readable claim (e.g. "Person.Information.Read"). Used in JWT
        // claims and code-level policy checks. Localized Name + Description live
        // on PermissionTranslation.
        [Required]
        [MaxLength(100)]
        [Unicode(false)]
        public string Code { get; set; } = string.Empty;

        // FK by value (cross-DB) to KSS_Common.dbo.Module(Id).
        public Guid ModuleId { get; set; }

        // FK by value (cross-DB) to KSS_Common.dbo.Resource(Id).
        public Guid ResourceId { get; set; }

        public ICollection<PermissionTranslation> Translations { get; set; } = new List<PermissionTranslation>();
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}
