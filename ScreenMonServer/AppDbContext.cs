using Microsoft.EntityFrameworkCore;

namespace ScreenMonServer
{
    internal class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Session> Sessions { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseLazyLoadingProxies().UseSqlite(@"Data Source=ScreenMonServer.db;Mode=ReadWriteCreate");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Session>().HasOne(s => s.User).WithMany(u => u.Sessions).HasForeignKey(s => s.UserId);
            base.OnModelCreating(modelBuilder);
        }
    }
}
