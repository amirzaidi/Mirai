using Mirai.Database.Tables;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace Mirai.Database
{
    class MiraiContext : DbContext
    {
        public DbSet<Config> Config { get; set; }
        public DbSet<DiscordCredentials> DiscordCredentials { get; set; }
        public DbSet<DiscordFeedlink> DiscordFeedlink { get; set; }
        public DbSet<Feed> Feed { get; set; }
        public DbSet<Song> Song { get; set; }
        public DbSet<TelegramCredentials> TelegramCredentials { get; set; }
        public DbSet<TelegramFeedlink> TelegramFeedlink { get; set; }

        internal MiraiContext() : base("name=Mirai")
        {
            Bot.Log("Opening database connection");
        }

        protected override void OnModelCreating(DbModelBuilder ModelBuilder)
        {
            ModelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
        }

        internal new void Dispose()
        {
            Bot.Log("Closing database connection");
            base.Dispose();
        }
    }
}
