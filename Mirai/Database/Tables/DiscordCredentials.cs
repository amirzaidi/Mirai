using System.ComponentModel.DataAnnotations;

namespace Mirai.Database.Tables
{
    class DiscordCredentials
    {
        [Key]
        public string App { get; set; }
        public string Token { get; set; }
    }
}
