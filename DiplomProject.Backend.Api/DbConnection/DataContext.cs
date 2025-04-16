using DiplomProject.DTOLibrary;
using Microsoft.EntityFrameworkCore;
using System.Threading;

namespace DiplomProject
{
    public class DataContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Document> Documents {  get; set; }
        public DbSet<DocFile> ImageFiles { get; set; }
        public DbSet<BugReport> BugReports { get; set; }
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
            modelBuilder.Entity<Document>()
                .HasMany(x => x.ImageFiles)
                .WithOne(x => x.Document);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .UseLazyLoadingProxies();
        }
    }
}
