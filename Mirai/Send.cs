namespace Mirai
{
    struct Destination
    {
        internal string Token;
        internal string Chat;
    }

    struct SendMessage
    {
        internal int Id;
        internal string Chat;
        internal string Text;
        internal object State;
        internal bool Markdown;
        internal string ReplyId;
    }
}
