using System.ComponentModel.DataAnnotations;

namespace Mirai.Database.Tables
{
    class TelegramFeedlink
    {
        [Key]
        public string Token { get; set; }
        [Key]
        public long Chat { get; set; }
        public int Feed { get; set; }
    }
}
