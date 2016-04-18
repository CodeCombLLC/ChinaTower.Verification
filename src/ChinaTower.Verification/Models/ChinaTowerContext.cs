using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Data.Entity;
using CodeComb.Data.Verification.EntityFramework;

namespace ChinaTower.Verification.Models
{
    public class ChinaTowerContext : IdentityDbContext<User>, IDataVerificationDbContext
    {
        public DbSet<DataVerificationRule> DataVerificationRules { get; set; }

        public DbSet<Form> Forms { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Form>(e =>
            {
                e.HasIndex(x => x.VerificationTime);
                e.HasIndex(x => x.Type);
                e.HasIndex(x => x.Status);
            });
        }
    }
}
