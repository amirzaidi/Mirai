using System.ComponentModel.DataAnnotations;

namespace Mirai.Database.Tables
{
    class TelegramCredentials
    {
        [Key]
        public string Token { get; set; }
    }
}
