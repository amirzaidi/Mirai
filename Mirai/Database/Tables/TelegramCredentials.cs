using System.ComponentModel.DataAnnotations;

namespace Mirai.Database.Tables
{
    class TelegramCredentials
    {
        [Key]
        public string Token { get; set; }
        public long Owner { get; set; }
    }
}
