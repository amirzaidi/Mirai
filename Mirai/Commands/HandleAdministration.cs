using System.Linq;
using System.Threading.Tasks;

namespace Mirai.Commands
{
    class Administration
    {
        internal static async Task MinRank(ReceivedMessage Message)
        {
            var Split = Message.Text.Split(' ');
            byte MinRank;
            if (Split.Length != 2 || !Parser.ValidKey(Split[0]) || !byte.TryParse(Split[1], out MinRank))
            {
                await Message.Respond("Invalid parameters given");
                return;
            }

            var Command = Split[0];
            using (var Context = Bot.GetDb)
            {
                var MinRankRow = Context.MinRank.Where(x => x.Command == Command).FirstOrDefault();
                if (MinRankRow == null)
                {
                    Context.MinRank.Add(new Database.Tables.MinRank
                    {
                        Feed = Message.Feed.Id,
                        Command = Split[0],
                        Rank = MinRank
                    });
                }
                else
                {
                    MinRankRow.Rank = MinRank;
                }

                await Context.SaveChangesAsync();
            }

            await Message.Respond($"Updated command {Command} to minrank {MinRank}");
        }

        internal static async Task GiveRank(ReceivedMessage Message)
        {
            var Split = Message.Text.Split(' ');
            byte GiveRank;
            if (Split.Length != 2 || Message.Mentions.Length != 1 || !byte.TryParse(Split[1], out GiveRank))
            {
                await Message.Respond("Invalid parameters given");
                return;
            }

            var UserToken = $"{Message.Origin.Token}/{Message.Mentions[0].Id}";
            using (var Context = Bot.GetDb)
            {
                var UserRow = Context.User.Where(x => x.UserToken == UserToken).FirstOrDefault();
                if (UserRow == null)
                {
                    Context.User.Add(new Database.Tables.User
                    {
                        UserToken = UserToken,
                        Rank = GiveRank
                    });
                }
                else
                {
                    UserRow.Rank = GiveRank;
                }

                await Context.SaveChangesAsync();
            }

            await Message.Respond($"Updated user {Message.Mentions[0].Mention} to rank {GiveRank}");
        }

        internal static async Task Rank(ReceivedMessage Message)
        {
            await Message.Respond($"You are rank {Message.SenderRank()}");
        }

        internal static async Task Id(ReceivedMessage Message)
        {
            await Message.Respond($"Your id is {Message.Sender}");
        }

        internal async Task JoinLink(ReceivedMessage Message)
        {
            var Info = await Bot.Clients[Message.Origin.Token].Info();
            await Message.Respond(Info.Join);
        }
    }
}
