using System.ComponentModel.DataAnnotations;

namespace Mirai.Database.Tables
{
    class User
    {
        [Key]
        public string Id { get; set; }
        public int Rank { get; set; }
    }
}
