using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KSS.Entity
{
    /// <summary>
    /// Localized name + description for a Role. Read-only from the app layer.
    /// </summary>
    [Table("RoleTranslation", Schema = "dbo")]
    public class RoleTranslation
    {
        public Guid RoleId { get; set; }
        public short LanguageId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Description { get; set; }

        [ForeignKey(nameof(RoleId))]
        public Role Role { get; set; } = null!;
    }
}
