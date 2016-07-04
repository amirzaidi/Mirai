using System.ComponentModel.DataAnnotations;

namespace Mirai.Database.Tables
{
    class DiscordFeedlink
    {
        [Key]
        public string App { get; set; }
        [Key]
        public string TextChannel { get; set; }
        public string VoiceChannel { get; set; }
        public int Feed { get; set; }
    }
}
