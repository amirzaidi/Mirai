using Mirai.Database.Tables;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using TelegramMessage = Telegram.Bot.Types.Message;

namespace Mirai.Client
{
    class Telegram : IClient
    {
        private TelegramBotClient Client;
        private ConcurrentDictionary<SendMessage, TelegramMessage> Sent = new ConcurrentDictionary<SendMessage, TelegramMessage>();
        private string Token;
        private string Mention;
        private long Owner;

        public bool Connected
        {
            get
            {
                return Client.TestApiAsync().Result;
            }
        }

        public Telegram(string Token, long Owner)
        {
            this.Token = Token;
            this.Owner = Owner;
        }

        public async Task Connect()
        {
            Client = new TelegramBotClient(Token);
            await UpdateCache();

            Client.OnMessage += OnMessage;
            Client.OnInlineQuery += OnInlineQuery;
            Client.StartReceiving();
        }

        public async Task Disconnect()
        {
            Client.StopReceiving();
        }

        public async Task Send(SendMessage Message)
        {
            string Text = Message.Text;
            Message.Text = null;

            if (!Sent.TryAdd(Message, await Client.SendTextMessageAsync(long.Parse(Message.Chat), Text)))
            {
                Bot.Log("Failed to add message to the telegram sent list");
            }
        }

        public async Task Edit(SendMessage Message)
        {
            string Text = Message.Text;
            Message.Text = null;

            TelegramMessage MessageObj;
            if (Sent.TryGetValue(Message, out MessageObj))
            {
                await Client.EditMessageTextAsync(MessageObj.Chat.Id.ToString(), MessageObj.MessageId, Text);
            }
        }

        public async Task Delete(SendMessage Message)
        {
        }

        public async Task Stream(string Chat, byte[] Sound)
        {
        }

        public async Task<ClientInformation> Info()
        {
            var UserData = await Client.GetMeAsync();
            return new ClientInformation()
            {
                Id = UserData.Id.ToString(),
                Name = UserData.Username,
                Type = typeof(Telegram)
            };
        }

        public async Task UpdateCache()
        {
            Mention = $"@{(await Info()).Name}";
            //Add Feedlink Updates
        }

        private async void OnMessage(object sender, MessageEventArgs e)
        {
            TelegramFeedlink FeedLink;

            //Remove this query
            using (var Context = Bot.GetDb)
            {
                FeedLink = (from Rows in Context.TelegramFeedlink
                            where Rows.Token == Token && Rows.Chat == e.Message.Chat.Id
                            select Rows).FirstOrDefault();
            }

            var Text = e.Message.Text;
            if (Text == null)
            {
                return;
            }

            byte JoinFeedId;
            if (FeedLink != null)
            {
                if (e.Message.From.Id == Owner && Text == Bot.Command + Bot.LeaveFeed + Mention)
                {
                    using (var Context = Bot.GetDb)
                    {
                        Context.TelegramFeedlink.Attach(FeedLink);
                        Context.TelegramFeedlink.Remove(FeedLink);
                        await Context.SaveChangesAsync();
                    }

                    Bot.UpdateCache();
                    Bot.Log($"Removed feed from {e.Message.Chat.Title} on {Mention}");
                    return;
                }

                var Message = new ReceivedMessage
                {
                    Origin = new Destination
                    {
                        Token = Token,
                        Chat = e.Message.Chat.Id.ToString()
                    },
                    Id = e.Message.MessageId,
                    Sender = e.Message.From.Id,
                    SenderMention = $"@{e.Message.From.Username}",
                    Text = e.Message.Text
                };

                var Trimmed = string.Empty;
                if (Message.Text.StartsWith(Bot.Command))
                {
                    Trimmed = Message.Text.Substring(Bot.Command);
                }

                if (Trimmed != string.Empty)
                {
                    Message.Command = Trimmed.Split(' ')[0];
                    if (Message.Command.Contains("@") && !Message.Command.EndsWith(Mention))
                    {
                        //Another bot was requested
                        Message.Command = null;
                    }
                    else
                    {
                        Message.Text = Trimmed.Substring(Message.Command).TrimStart();
                        Message.Command = Message.Command.Replace(Mention, "");
                    }
                }

                Bot.Feeds[FeedLink.Feed].Handle(Message);
            }
            else if (e.Message.From.Id == Owner)
            {
                var JoinFeed = Bot.Command + Bot.JoinFeed + Mention + " ";

                if (Text.StartsWith(JoinFeed) && byte.TryParse(Text.Substring(JoinFeed), out JoinFeedId) && JoinFeedId < Bot.Feeds.Length)
                {
                    using (var Context = Bot.GetDb)
                    {
                        Context.TelegramFeedlink.Add(new TelegramFeedlink()
                        {
                            Token = Token,
                            Chat = e.Message.Chat.Id,
                            Feed = JoinFeedId
                        });

                        await Context.SaveChangesAsync();
                    }

                    Bot.UpdateCache();
                    Bot.Log($"Added feed to {e.Message.Chat.Title} on {Mention}");
                }
            }
        }

        private void OnInlineQuery(object sender, InlineQueryEventArgs e)
        {
            Bot.Log("Inline " + e.InlineQuery.Query);
            //Special handler
        }
    }
}
