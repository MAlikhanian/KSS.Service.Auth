using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace KSS.Data.DbContexts
{
    public partial class MainDbContext : DbContext
    {
        // Synthetic system Guid used when a current-user context is not available.
        private static readonly Guid SystemUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        public MainDbContext(DbContextOptions<MainDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            base.OnModelCreating(modelBuilder);
        }

        public override int SaveChanges()
        {
            ApplyEntityDefaults();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyEntityDefaults();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void ApplyEntityDefaults()
        {
            var now = DateTime.UtcNow;

            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.State == EntityState.Added)
                {
                    var idProp = entry.Entity.GetType().GetProperty("Id");
                    if (idProp != null && idProp.PropertyType == typeof(Guid))
                    {
                        var currentId = (Guid)idProp.GetValue(entry.Entity)!;
                        if (currentId == Guid.Empty)
                            idProp.SetValue(entry.Entity, Guid.CreateVersion7());
                    }

                    var createdAtProp = entry.Entity.GetType().GetProperty("CreatedAt");
                    if (createdAtProp != null)
                        createdAtProp.SetValue(entry.Entity, now);

                    var createdByProp = entry.Entity.GetType().GetProperty("CreatedBy");
                    if (createdByProp != null && createdByProp.PropertyType == typeof(Guid))
                    {
                        var currentCreatedBy = (Guid)createdByProp.GetValue(entry.Entity)!;
                        if (currentCreatedBy == Guid.Empty)
                            createdByProp.SetValue(entry.Entity, SystemUserId);
                    }
                    // UpdatedAt and UpdatedBy stay NULL on insert — they're only set on actual updates.
                }

                if (entry.State == EntityState.Modified)
                {
                    var updatedAtProp = entry.Entity.GetType().GetProperty("UpdatedAt");
                    if (updatedAtProp != null)
                        updatedAtProp.SetValue(entry.Entity, now);
                }
            }
        }
    }
}
