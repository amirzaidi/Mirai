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

        public bool Connected
        {
            get
            {
                return Client.TestApiAsync().Result;
            }
        }

        public Telegram(string Token)
        {
            this.Token = Token;
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
                Type = typeof(Telegram)
            };
        }

        private void OnMessage(object sender, MessageEventArgs e)
        {
            int FeedId;
            using (var Context = Bot.GetDb)
            {
                FeedId = (from Rows
                    in Context.TelegramFeedlink
                    where Rows.Token == Token && Rows.Chat == e.Message.Chat.Id
                    select Rows.Feed)
                    .FirstOrDefault();
            }

            if (FeedId > 0)
            {
                Bot.Feeds[FeedId].Handle(this, new Message()
                {
                    Type = typeof(Telegram),
                    Id = e.Message.MessageId,
                    Sender = e.Message.From.Id,
                    SenderMention = $"@{e.Message.From.Username}",
                    Text = e.Message.Text
                });
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
