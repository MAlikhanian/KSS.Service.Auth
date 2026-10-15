using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using KSS.Entity;

namespace KSS.Data.Configuration
{
    public class PermissionTranslationConfiguration : IEntityTypeConfiguration<PermissionTranslation>
    {
        public void Configure(EntityTypeBuilder<PermissionTranslation> b)
        {
            b.HasKey(t => new { t.PermissionId, t.LanguageId });
            b.HasOne(t => t.Permission)
                .WithMany(p => p.Translations)
                .HasForeignKey(t => t.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
