using System;
using System.Threading.Tasks;

namespace Mirai.Commands
{
    class Command
    {
        public enum PrefixType
        {
            Command,
            None
        }

        public PrefixType Prefix;
        public string[] Keys;
        public string Description;
        public Func<ReceivedMessage, Task> Handler;

        public Command(PrefixType Prefix, string Key, string Description, string Response)
            : this(Prefix, new string[] { Key }, Description, Response)
        {
        }

        public Command(PrefixType Prefix, string[] Keys, string Description, string Response)
            : this(Prefix, Keys, Description, async (e) =>
            {
                e.Respond(Response);
            })
        {
        }

        public Command(PrefixType Prefix, string Key, string Description, Func<ReceivedMessage, Task> Handler)
            : this(Prefix, new string[] { Key }, Description, Handler)
        {
        }

        public Command(PrefixType Prefix, string[] Keys, string Description, Func<ReceivedMessage, Task> Handler)
        {
            this.Prefix = Prefix;
            this.Keys = Keys;
            this.Description = Description;
            this.Handler = Handler;
        }
    }
}
