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
        public DbSet<Song> Song { get; set; }
        public DbSet<TelegramCredentials> TelegramCredentials { get; set; }
        public DbSet<TelegramFeedlink> TelegramFeedlink { get; set; }

        internal MiraiContext() : base("name=Mirai")
        {
        }

        protected override void OnModelCreating(DbModelBuilder ModelBuilder)
        {
            ModelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
        }
    }
}
