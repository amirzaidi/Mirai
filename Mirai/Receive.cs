using System.Linq;
using System.Threading.Tasks;

namespace Mirai
{
    struct ReceivedMessageMention
    {
        internal string Id;
        internal string Mention;
    }

    struct ReceivedMessage
    {
        internal FeedContext Feed;
        internal Destination Origin;
        internal string MessageId;
        internal string Sender;
        internal string SenderMention;
        internal string Command;
        internal string Text;
        internal ReceivedMessageMention[] Mentions;
        internal object State;
        internal string UserToken
        {
            get
            {
                return $"{Origin.Token}/{Sender}";
            }
        }

        internal async Task Respond(string Text, bool Markdown = true)
        {
            Feed.Send(Origin, Text, Markdown, State, ReplyId: MessageId);
        }

        internal byte SenderRank()
        {
            if (Sender == Bot.Clients[Origin.Token].Owner)
            {
                return byte.MaxValue;
            }

            const byte DefaultRank = 1;

            using (var Context = Bot.GetDb)
            {
                var UserToken = this.UserToken;
                var Users = Context.User.Where(x => x.UserToken == UserToken).Select(x => x.Rank);
                if (Users.Count() > 0)
                {
                    return Users.First();
                }

                Context.User.Add(new Database.Tables.User
                {
                    UserToken = UserToken,
                    Rank = DefaultRank
                });
            }

            return DefaultRank;
        }
    }
}
