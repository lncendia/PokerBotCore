using Microsoft.EntityFrameworkCore;

namespace PokerBot
{
    class DB : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Friendship> Friendships { get; set; }

        public DB()
        {
            Database.EnsureCreated();
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Filename=refull.sqlite");
        }
    }
}
