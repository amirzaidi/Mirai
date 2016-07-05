using System;

namespace Mirai
{
    struct Message
    {
        public Type Type;
        public object Id;
        public object Sender;
        public string SenderMention;
        public string Command;
        public string Text;
    }
}
