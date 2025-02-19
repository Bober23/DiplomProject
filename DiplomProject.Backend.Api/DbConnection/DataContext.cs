using DiplomProject.DTOLibrary;
using Microsoft.EntityFrameworkCore;
using System.Threading;

namespace DiplomProject
{
    public class DataContext : DbContext
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<Document> Documents => Set<Document>();
        public DataContext()
        {
            Database.EnsureCreated();
        }
        public DataContext(DbContextOptions<DataContext> options)
        : base(options)
        {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);
            Database.EnsureCreated();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasMany(x => x.Documents)
                .WithOne(x => x.User);
        }
    }
}
