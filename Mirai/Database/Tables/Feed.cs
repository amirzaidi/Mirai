using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;

namespace Mirai.Database.Tables
{
    class Feed
    {
        [Key, Column("id")]
        public int Id { get; set; }

        public bool Administration { get; set; }
        public bool Audio { get; set; }
        public bool Conversation { get; set; }
        public bool Lewd { get; set; }
        public bool Search { get; set; }
        public bool Trivia { get; set; }

        public bool IsEnabled(string Category)
        {
            return (bool)(GetType().GetProperty(Category)?.GetValue(this, null) ?? false);
        }

        public async Task<bool> Toggle(string Category)
        {
            var PropertyInfo = GetType().GetProperty(Category);
            if (PropertyInfo == null)
            {
                return false;
            }

            var ToEnable = !(bool)PropertyInfo.GetValue(this);
            using (var Context = Bot.GetDb)
            {
                Context.Feed.Attach(this);
                PropertyInfo.SetValue(this, ToEnable);
                await Context.SaveChangesAsync();
            }

            return ToEnable;
        }
    }
}
