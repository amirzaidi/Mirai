using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mirai.Database.Tables
{
    class TelegramFeedlink
    {
        [Key, Column(Order = 1)]
        public string Token { get; set; }

        [Key, Column(Order = 2)]
        public long Chat { get; set; }
        public int Feed { get; set; }

        //[ForeignKey("Feed")]
        //public virtual Feed FeedObj { get; set; }
    }
}
