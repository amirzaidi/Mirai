using System;
using System.Threading.Tasks;

namespace Mirai
{
    struct ReceivedMessage
    {
        internal FeedContext Feed;
        internal Destination Origin;
        internal object Id;
        internal object Sender;
        internal string SenderMention;
        internal string Command;
        internal string Text;

        internal async Task Respond(string Text)
        {
            Feed.Send(Origin, Text);
        }
    }
}
