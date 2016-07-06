using Discord;
using DiscordMessage = Discord.Message;
using Mirai.Database.Tables;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Discord.Audio;
using System.Collections.Generic;

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
            try
            {
                await Client.Connect(Token);
                await UpdateCache();

                Client.MessageReceived += MessageReceived;
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
                string Text = Message.Text;
                Message.Text = null;

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

        public async Task Stream(string Chat, byte[] Sound)
        {
            var ChatId = ulong.Parse(Chat);

            IAudioClient AudioClient;
            if (!AudioClients.TryGetValue(ChatId, out AudioClient))
            {
                var Channel = Client.Servers.SelectMany(x => x.VoiceChannels).Where(x => x.Id == ChatId).FirstOrDefault();
                if (Channel == null)
                {
                    return;
                }

                AudioClient = await Channel.JoinAudio();
                if (!AudioClients.TryAdd(ChatId, AudioClient))
                {
                    //Bot.Log("Failed to add audio client to the Audioclient list");
                    await Task.Delay(100);
                    return;
                }
            }

            if (AudioClient.State != ConnectionState.Connected)
            {
                await AudioClient.Join(AudioClient.Channel);
            }

            if (Sound != null)
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
                Type = typeof(Discord)
            };
        }

        public async Task UpdateCache()
        {
            Mention = $"<@{Client.CurrentUser.Id}>";
            //Add Cached Feedlist
        }

        private async void MessageReceived(object sender, MessageEventArgs e)
        {
            if (!e.User.IsBot && e.Message.Text != string.Empty)
            {
                string TextChannel = e.Channel.Id.ToString();
                DiscordFeedlink FeedLink;

                //Remove this query
                using (var Context = Bot.GetDb)
                {
                    FeedLink = Context.DiscordFeedlink.Where(x => x.Token == Token && x.TextChannel == TextChannel).FirstOrDefault();
                }

                string RawText = e.Message.RawText;
                byte JoinFeedId;
                if (FeedLink != null)
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
                        else if (RawText == Mention + " setaudio")
                        {
                            using (var Context = Bot.GetDb)
                            {
                                Context.DiscordFeedlink.Attach(FeedLink);
                                FeedLink.VoiceChannel = e.User.VoiceChannel?.Id.ToString();
                                await Context.SaveChangesAsync();
                            }

                            Bot.UpdateCache();
                            Bot.Log($"Set audio to {e.User.VoiceChannel?.Name} on {Mention}");
                            return;
                        }
                    }

                    var Message = new ReceivedMessage
                    {
                        Origin = new Destination
                        {
                            Token = Token,
                            Chat = e.Channel.Id.ToString()
                        },
                        Id = e.User.Id,
                        Sender = e.User.Id.ToString(),
                        SenderMention = $"<@{e.User.Id}>",
                        Text = RawText,
                        Mentions = e.Message.MentionedUsers.Where(x => !x.IsBot).Select(x => new ReceivedMessageMention
                        {
                            Id = x.Id.ToString(),
                            Mention = $"<@{x.Id}>"
                        }).ToArray(),
                        State = null
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
                    
                    Bot.Feeds[FeedLink.Feed].Handle(Message);
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
    }
}
