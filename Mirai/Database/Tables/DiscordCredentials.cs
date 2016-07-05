using System.ComponentModel.DataAnnotations;

namespace Mirai.Database.Tables
{
    class DiscordCredentials
    {
        [Key]
        public string Token { get; set; }
        public string App { get; set; }
        public string Owner { get; set; }
    }
}
