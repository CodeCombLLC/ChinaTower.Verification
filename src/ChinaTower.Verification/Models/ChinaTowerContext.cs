using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Data.Entity;
using CodeComb.Data.Verification.EntityFramework;

namespace ChinaTower.Verification.Models
{
    public class ChinaTowerContext : IdentityDbContext<User>, IDataVerificationDbContext
    {
        public DbSet<DataVerificationRule> DataVerificationRules { get; set; }

        public DbSet<Form> Forms { get; set; }

        public DbSet<City> Cities { get; set; }

        public DbSet<VerificationRule> VerificationRules { get; set; }

        public DbSet<Log> Logs { get; set; }

        public DbSet<Blob> Blobs { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Form>(e =>
            {
                e.HasIndex(x => x.VerificationTime);
                e.HasIndex(x => x.Type);
                e.HasIndex(x => x.Status);
                e.HasIndex(x => x.Lon);
                e.HasIndex(x => x.Lat);
                e.HasIndex(x => x.UniqueKey);
                e.HasIndex(x => x.City);
                e.HasIndex(x => x.District);
                e.HasIndex(x => x.StationKey);
                e.HasIndex(x => x.Name);
            });

            builder.Entity<VerificationRule>(e =>
            {
                e.HasIndex(x => x.Type);
            });

            builder.Entity<Log>(e =>
            {
                e.HasIndex(x => x.Time);
            });

            builder.Entity<Blob>(e =>
            {
                e.HasIndex(x => x.Time);
            });
        }
    }
}
