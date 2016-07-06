using System;
using System.Linq;
using System.Threading.Tasks;

namespace Mirai.Commands
{
    internal enum CommandType
    {
        Command,
        Text
    }

    class Command
    {
        internal CommandType Prefix;
        internal string[] Keys;
        internal string Description;
        internal Func<ReceivedMessage, Task> Handler;

        internal Command(CommandType Prefix, string Key, string Description, string Response)
            : this(Prefix, new string[] { Key }, Description, Response)
        {
        }

        internal Command(CommandType Prefix, string[] Keys, string Description, string Response)
            : this(Prefix, Keys, Description, async (Message) =>
            {
                await Message.Respond(Response);
            })
        {
        }

        internal Command(CommandType Prefix, string Key, string Description, Func<ReceivedMessage, Task> Handler)
            : this(Prefix, new string[] { Key }, Description, Handler)
        {
        }

        internal Command(CommandType Prefix, string[] Keys, string Description, Func<ReceivedMessage, Task> Handler)
        {
            this.Prefix = Prefix;
            this.Keys = Keys;
            this.Description = Description;
            this.Handler = async delegate (ReceivedMessage Message)
            {
                if (Message.SenderRank() >= MinRank(Message.Feed.Id))
                {
                    Handler(Message);
                }
            };
        }

        internal byte MinRank(byte Feed)
        {
            const byte DefaultMinRank = 1;

            if (Prefix == CommandType.Text)
            {
                return DefaultMinRank;
            }

            using (var Context = Bot.GetDb)
            {
                var Row = Context.MinRank.Where(x => Keys.Contains(x.Command) && x.Feed == Feed).Select(x => x.Rank);

                if (Row.Count() > 0)
                {
                    return Row.First();
                }

                Context.MinRank.Add(new Database.Tables.MinRank
                {
                    Feed = Feed,
                    Command = Keys[0],
                    Rank = DefaultMinRank
                });

                return DefaultMinRank;
            }
        }
    }
}
