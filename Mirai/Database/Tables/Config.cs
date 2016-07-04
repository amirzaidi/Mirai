using System.ComponentModel.DataAnnotations;

namespace Mirai.Database.Tables
{
    class Config
    {
        [Key]
        public string Name { get; set; }
        public string Value { get; set; }
    }
}
