using Discord;
using System;
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
                Type = "Discord"
            };
        }

        private void MessageReceived(object sender, MessageEventArgs e)
        {
            Bot.Log("Discord " + e.Message.Text);
            //Feed handler
        }
    }
}
