using System.ComponentModel.DataAnnotations;

namespace Mirai.Database.Tables
{
    class Feed
    {
        [Key]
        public string Id { get; set; }
        public string Name { get; set; }
        
        public void HandleCommand(string Cmd, string Args)
        {

        }

        public void HandleText(string Cmd, string Args)
        {

        }
    }
}
