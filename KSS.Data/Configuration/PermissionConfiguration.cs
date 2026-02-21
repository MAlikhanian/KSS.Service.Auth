using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using KSS.Entity;

namespace KSS.Data.Configuration
{
    public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
    {
        public void Configure(EntityTypeBuilder<Permission> b)
        {
            b.HasIndex(p => p.Name).IsUnique();
        }
    }
}
