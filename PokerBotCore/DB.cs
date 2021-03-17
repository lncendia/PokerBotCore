using Microsoft.EntityFrameworkCore;
using PokerBotCore.Model;

namespace PokerBotCore
{
    internal sealed class Db : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Friendship> Friendships { get; set; }

        public Db()
        {
            Database.EnsureCreated();
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Filename=user.sqlite");
        }
    }
}
