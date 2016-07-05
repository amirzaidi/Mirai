using Discord;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Mirai.Client
{
    class Discord : IClient
    {
        private DiscordClient Client = new DiscordClient();
        private string App;
        private string Token;

        public bool Connected
        {
            get
            {
                return Client.State == ConnectionState.Connected;
            }
        }

        public Discord(string App, string Token)
        {
            this.App = App;
            this.Token = Token;
        }

        async Task IClient.Connect()
        {
            try
            {
                await Client.Connect(Token);
                Client.MessageReceived += MessageReceived;
            }
            catch (Exception Ex)
            {
                Bot.Log(Ex);
            }
        }

        async Task IClient.Disconnect()
        {
            try
            {
                await Client.Disconnect();
            }
            catch (Exception Ex)
            {
                Bot.Log(Ex);
            }
        }

        async Task<ClientInformation> IClient.Info()
        {
            return new ClientInformation()
            {
                Id = Client.CurrentUser.Id.ToString(),
                Name = Client.CurrentUser.Name,
                Type = typeof(Discord)
            };
        }

        private void MessageReceived(object sender, MessageEventArgs e)
        {
            int FeedId;
            using (var Context = Bot.GetDb)
            {
                FeedId = (from Rows 
                    in Context.DiscordFeedlink
                    where Rows.App == App && Rows.TextChannel == e.Channel.Id.ToString()
                    select Rows.Feed)
                    .FirstOrDefault();
            }

            if (FeedId > 0)
            {
                string Command = null;
                string Args = null;

                Bot.Feeds[FeedId].Handle(this, new Message()
                {
                    Type = typeof(Discord),
                    Id = e.User.Id,
                    Sender = e.User.Id,
                    SenderMention = $"@{e.User.Name}",
                    Text = e.Message.Text
                }, Command, Args);
            }

            Bot.Log("Discord " + e.Message.Text);
            //Feed handler
        }
    }
}
