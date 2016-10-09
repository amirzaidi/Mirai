using Discord;
using DiscordMessage = Discord.Message;
using Mirai.Database.Tables;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Discord.Audio;
using Mirai.Commands;

namespace Mirai.Client
{
    class Discord : IClient
    {
        private DiscordClient Client = new DiscordClient();
        private ConcurrentDictionary<SendMessage, DiscordMessage> Sent = new ConcurrentDictionary<SendMessage, DiscordMessage>();
        private ConcurrentDictionary<ulong, IAudioClient> AudioClients = new ConcurrentDictionary<ulong, IAudioClient>();
        private string App;
        private string Token;
        private string Mention;
        public string Owner { get; set; }

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

            Client.UsingAudio(new AudioServiceConfigBuilder()
            {
                Channels = 2,
                EnableEncryption = false,
                Bitrate = AudioServiceConfig.MaxBitrate,
                BufferLength = 1000,
                Mode = AudioMode.Outgoing
            }.Build());
        }

        public async Task Connect()
        {
            Client.MessageReceived += MessageReceived;
            //Client.UserJoined += UserJoined;
            //Client.UserLeft += UserLeft;
            Client.Log.Message += delegate (object sender, LogMessageEventArgs e)
            {
                var Text = $"{e.Severity} ";
                if (e.Message != null)
                    Text += $"Message {e.Message}";

                if (e.Exception != null)
                    Text += $"\nException {e.Exception}";

                if (e.Source != null)
                    Text += $" From {e.Source}";

                Bot.Log(Text);
            };

            try
            {
                await Client.Connect(Token, TokenType.Bot);
                await UpdateCache();
            }
            catch (Exception Ex)
            {
                Bot.Log(Ex);
            }
        }

        public async Task Disconnect()
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

        public async Task Send(SendMessage Message)
        {
            var ChatId = ulong.Parse(Message.Chat);
            var Channel = Client.Servers.SelectMany(x => x.TextChannels).Where(x => x.Id == ChatId).FirstOrDefault();
            if (Channel != null)
            {
                var Text = Message.Text;
                Message.Text = null;

                if (Text.Length > 2000)
                {
                    Text = $"{Text.Substring(0, 1997)}...";
                }

                DiscordMessage Old;
                Sent.TryRemove(Message, out Old);
                if (!Sent.TryAdd(Message, await Channel.SendMessage(Text)))
                {
                    Bot.Log("Failed to add message to the discord sent list");
                }
            }
        }

        public async Task Edit(SendMessage Message)
        {
            string Text = Message.Text;

            Message.Text = null;
            DiscordMessage MessageObj;
            if (Sent.TryGetValue(Message, out MessageObj))
            {
                await MessageObj.Edit(Text);
            }
        }

        public async Task Delete(SendMessage Message)
        {
            Message.Text = null;
            DiscordMessage MessageObj;
            if (Sent.TryRemove(Message, out MessageObj))
            {
                await MessageObj.Delete();
            }
        }

        private static bool SendAudio = false;
        public async Task Stream(string Chat, byte[] Sound)
        {
            var ChatId = ulong.Parse(Chat);

            IAudioClient AudioClient;
            if (!AudioClients.TryGetValue(ChatId, out AudioClient) || AudioClient.Channel.Id != ChatId)
            {
                var Channel = Client.Servers.SelectMany(x => x.VoiceChannels).Where(x => x.Id == ChatId).FirstOrDefault();
                if (Channel == null)
                {
                    return;
                }
                
                AudioClient = await Channel.JoinAudio();

                IAudioClient Old;
                AudioClients.TryRemove(ChatId, out Old);
                if (AudioClients.TryAdd(ChatId, AudioClient))
                {
                    SendAudio = false;
                    Task.Delay(750).ContinueWith(delegate { SendAudio = true; });
                }
            }
            else if (AudioClient.State != ConnectionState.Connected)
            {
                await AudioClient.Join(AudioClient.Channel);
            }
            else if (Sound != null && SendAudio)
            {
                try
                {
                    await AudioClient.OutputStream.WriteAsync(Sound, 0, Sound.Length);
                }
                catch (Exception Ex)
                {
                    Bot.Log(Ex);
                }
            }
        }

        public async Task<ClientInformation> Info()
        {
            await Task.Yield();
            return new ClientInformation()
            {
                Id = Client.CurrentUser.Id.ToString(),
                Name = Client.CurrentUser.Name,
                Join = $"https://discordapp.com/oauth2/authorize?&client_id={App}&scope=bot",
                Type = typeof(Discord)
            };
        }

        private ConcurrentDictionary<string, DiscordFeedlink> FeedLinks = new ConcurrentDictionary<string, DiscordFeedlink>();

        public async Task UpdateCache()
        {
            Mention = Client.CurrentUser.Mention;

            FeedLinks.Clear();
            using (var Context = Bot.GetDb)
            {
                foreach (var FeedLink in Context.DiscordFeedlink.Where(x => x.Token == Token))
                {
                    if (!FeedLinks.TryAdd(FeedLink.TextChannel, FeedLink))
                    {
                        Bot.Log("Can't add discord feedlink");
                    }
                }
            }
        }

        private async void MessageReceived(object sender, MessageEventArgs e)
        {
            try
            {
                if (!e.User.IsBot && e.Message.Text != string.Empty)
                {
                    var TextChannel = e.Channel.Id.ToString();
                    DiscordFeedlink FeedLink;

                    var RawText = e.Message.RawText;
                    byte JoinFeedId;
                    if (FeedLinks.TryGetValue(TextChannel, out FeedLink))
                    {
                        if (e.User.Id.ToString() == Owner)
                        {
                            if (RawText == Mention + " " + Bot.LeaveFeed)
                            {
                                using (var Context = Bot.GetDb)
                                {
                                    Context.DiscordFeedlink.Attach(FeedLink);
                                    Context.DiscordFeedlink.Remove(FeedLink);
                                    await Context.SaveChangesAsync();
                                }

                                Bot.UpdateCache();
                                Bot.Log($"Removed feed from {e.Channel.Name} on {Mention}");
                                return;
                            }
                            //Start Discord-specific commands
                            else if (RawText.StartsWith(Mention + " changename"))
                            {
                                var Name = RawText.Substring(Mention + " changename").Trim();
                                if (Name.Length != 0)
                                {
                                    await Client.CurrentUser.Edit(username: Name);
                                }
                            }
                            else if (RawText == Mention + " setaudio")
                            {
                                var Success = false;
                                var ServerVoiceChannelIds = e.Server.VoiceChannels.Select(y => y.Id.ToString()).ToList();
                                var TextChannelId = e.Channel.Id.ToString();

                                using (var Context = Bot.GetDb)
                                {
                                    if (!Context.DiscordFeedlink.Any(x => x.TextChannel != TextChannelId && ServerVoiceChannelIds.Contains(x.VoiceChannel)))
                                    {
                                        Context.DiscordFeedlink.Attach(FeedLink);
                                        FeedLink.VoiceChannel = e.User.VoiceChannel?.Id.ToString();
                                        await Context.SaveChangesAsync();
                                        Success = true;

                                    }
                                }

                                if (Success)
                                {
                                    Bot.UpdateCache();
                                    Bot.Log($"Set audio to {e.User.VoiceChannel?.Name} on {Mention}");
                                }
                                else
                                {
                                    Bot.Log($"Can't set audio to {e.User.VoiceChannel?.Name} on {Mention}");
                                }

                                return;
                            }
                        }

                        var Message = new ReceivedMessage
                        {
                            Feed = Bot.Feeds[FeedLink.Feed],
                            Origin = new Destination
                            {
                                Token = Token,
                                Chat = e.Channel.Id.ToString()
                            },
                            MessageId = e.Message.Id.ToString(),
                            Sender = e.User.Id.ToString(),
                            SenderMention = $"<@{e.User.Id}>",
                            Text = RawText,
                            Mentions = e.Message.MentionedUsers.Where(x => !x.IsBot).Select(x => new ReceivedMessageMention
                            {
                                Id = x.Id.ToString(),
                                Mention = $"<@{x.Id}>"
                            }).ToArray()
                        };

                        var Trimmed = string.Empty;
                        if (Message.Text.StartsWith(Mention))
                        {
                            Trimmed = Message.Text.Substring(Mention).TrimStart();
                        }
                        else if (Message.Text.StartsWith(Bot.Command))
                        {
                            Trimmed = Message.Text.Substring(Bot.Command);
                        }

                        if (Trimmed != string.Empty)
                        {
                            Message.Command = Trimmed.Split(' ')[0];
                            Message.Text = Trimmed.Substring(Message.Command).TrimStart();
                        }

                        await Parser.Parse(Message);
                    }
                    else if (e.User.Id.ToString() == Owner)
                    {
                        var JoinFeed = Mention + " " + Bot.JoinFeed + " ";
                        if (RawText.StartsWith(JoinFeed) && byte.TryParse(RawText.Substring(JoinFeed), out JoinFeedId) && JoinFeedId < Bot.Feeds.Length)
                        {
                            using (var Context = Bot.GetDb)
                            {
                                Context.DiscordFeedlink.Add(new DiscordFeedlink()
                                {
                                    Token = Token,
                                    TextChannel = e.Channel.Id.ToString(),
                                    Feed = JoinFeedId
                                });

                                await Context.SaveChangesAsync();
                            }

                            Bot.UpdateCache();
                            Bot.Log($"Added feed to {e.Channel.Name} on {Mention}");
                        }
                    }
                }
            }
            catch (Exception Ex)
            {
                Bot.Log(Ex);
            }
        }

        private async void UserJoined(object sender, UserEventArgs e)
        {
            foreach (var TextChannel in e.Server.TextChannels.Where(x => FeedLinks.Values.Any(y => y.TextChannel == x.Id.ToString())))
            {
                await TextChannel.SendMessage($"Welcome, {e.User.Mention}!");
            }
        }

        private async void UserLeft(object sender, UserEventArgs e)
        {
            foreach (var TextChannel in e.Server.TextChannels.Where(x => FeedLinks.Values.Any(y => y.TextChannel == x.Id.ToString())))
            {
                await TextChannel.SendMessage($"Bye, {e.User.Name}!");
            }
        }
    }
}
