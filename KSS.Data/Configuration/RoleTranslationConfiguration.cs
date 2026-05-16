using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using KSS.Entity;

namespace KSS.Data.Configuration
{
    public class RoleTranslationConfiguration : IEntityTypeConfiguration<RoleTranslation>
    {
        public void Configure(EntityTypeBuilder<RoleTranslation> b)
        {
            b.HasKey(t => new { t.RoleId, t.LanguageId });
            b.HasOne(t => t.Role)
                .WithMany(r => r.Translations)
                .HasForeignKey(t => t.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
