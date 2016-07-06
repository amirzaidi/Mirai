using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mirai.Database.Tables
{
    class MinRank
    {
        [Key, Column(Order = 1)]
        public byte Feed { get; set; }

        [Key, Column(Order = 2)]
        public string Command { get; set; }
        public byte Rank { get; set; }
    }
}
