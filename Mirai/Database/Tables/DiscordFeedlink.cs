using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mirai.Database.Tables
{
    class DiscordFeedlink
    {
        [Key, Column(Order = 1)]
        public string App { get; set; }

        [Key, Column(Order = 2)]
        public string TextChannel { get; set; }
        public string VoiceChannel { get; set; }
        public int Feed { get; set; }

        //[ForeignKey("Feed")]
        //public virtual Feed FeedObj { get; set; }
    }
}
