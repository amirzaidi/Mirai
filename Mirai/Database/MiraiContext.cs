using Mirai.Database.Tables;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Threading.Tasks;

namespace Mirai.Database
{
    class MiraiContext : DbContext
    {
        public DbSet<Config> Config { get; set; }
        public DbSet<DiscordCredentials> DiscordCredentials { get; set; }
        public DbSet<DiscordFeedlink> DiscordFeedlink { get; set; }
        public DbSet<Feed> Feed { get; set; }
        public DbSet<MinRank> MinRank { get; set; }
        public DbSet<Song> Song { get; set; }
        public DbSet<TelegramCredentials> TelegramCredentials { get; set; }
        public DbSet<TelegramFeedlink> TelegramFeedlink { get; set; }
        public DbSet<User> User { get; set; }

        internal MiraiContext() : base("name=Mirai")
        {
        }

        protected override void OnModelCreating(DbModelBuilder ModelBuilder)
        {
            ModelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
        }

        public override async Task<int> SaveChangesAsync()
        {
            do
            {
                try
                {
                    return await base.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException Ex)
                {
                    Bot.Log(Ex.ToString());

                    // Update the values of the entity that failed to save from the store
                    foreach (var Entry in Ex.Entries)
                    {
                        await Entry.ReloadAsync();
                    }
                }
            } while (true);
        }
    }
}
