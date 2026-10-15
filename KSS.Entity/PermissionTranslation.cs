using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KSS.Entity
{
    /// <summary>
    /// Localized name/description for a Permission. Read-only from the app layer.
    /// </summary>
    [Table("PermissionTranslation", Schema = "dbo")]
    public class PermissionTranslation
    {
        public Guid PermissionId { get; set; }
        public short LanguageId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Description { get; set; }

        [ForeignKey(nameof(PermissionId))]
        public Permission Permission { get; set; } = null!;
    }
}
