using System.ComponentModel.DataAnnotations;

namespace Mirai.Database.Tables
{
    class User
    {
        [Key]
        public string UserToken { get; set; }
        public byte Rank { get; set; }
    }
}
