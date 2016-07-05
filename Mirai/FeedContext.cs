using Mirai.Client;

namespace Mirai
{
    class FeedContext
    {
        private int Id;

        internal FeedContext(int Id)
        {
            this.Id = Id;
        }

        internal void StartHandler()
        {
            //Music, trivia, etc
        }

        internal void Handle(IClient Client, Message Message)
        {

        }
    }
}
