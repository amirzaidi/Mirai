using System.ComponentModel.DataAnnotations;

namespace Mirai.Database.Tables
{
    class Song
    {
        [Key]
        public int Feed { get; set; }
        [Key]
        public int Place { get; set; }
        public string Query { get; set; }
    }
}
