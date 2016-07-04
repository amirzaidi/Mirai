using System;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;

namespace Mirai.Client
{
    class Telegram : IClient
    {
        private TelegramBotClient Client;
        private string Token;

        public Telegram(string Token)
        {
            this.Token = Token;
        }

        public bool Connected
        {
            get
            {
                return Client.TestApiAsync().Result;
            }
        }

        async Task IClient.Connect()
        {
            Client = new TelegramBotClient(Token);
            Client.OnMessage += OnMessage;
            Client.OnInlineQuery += OnInlineQuery;

            Client.StartReceiving();
        }

        async Task IClient.Disconnect()
        {
            Client.StopReceiving();
        }

        async Task<ClientInformation> IClient.Info()
        {
            var UserData = await Client.GetMeAsync();
            return new ClientInformation()
            {
                Id = UserData.Id.ToString(),
                Name = UserData.Username,
                Type = "Telegram"
            };
        }

        private void OnMessage(object sender, MessageEventArgs e)
        {
            using (var Context = Bot.GetDb)
            {
                var Feed = from Rows in Context.Feed where Rows.Id == "5" select Rows;
            }

            Bot.Log("TG " + e.Message.Text);
            //Feed handler
        }

        private void OnInlineQuery(object sender, InlineQueryEventArgs e)
        {
            Bot.Log("Inline " + e.InlineQuery.Query);
            //Special handler
        }
    }
}
