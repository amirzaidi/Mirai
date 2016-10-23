using Mirai.Commands;
using Mirai.Database.Tables;
using Mirai.Handlers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mirai
{
    class FeedContext
    {
        internal static int HandlersRunning = 0;
        private static int CurrentMessage = 1;

        internal byte Id
        {
            get;
            private set;
        }
        
        internal Destination[] TextDestination = new Destination[0];
        internal Destination[] AudioDestination = new Destination[0];
        internal MusicHandler Music;
        internal TriviaHandler Trivia;
        internal Task HandleTask;
        internal Feed FeedInfo;

        internal FeedContext(byte Id)
        {
            this.Id = Id;
            HandleTask = StartHandler();
        }

        private async Task StartHandler()
        {
            await Task.Yield();

            var Handlers = new List<IHandler>();

            Music = new MusicHandler(this);
            Handlers.Add(Music);

            Trivia = new TriviaHandler(this);
            Handlers.Add(Trivia);

            Interlocked.Increment(ref HandlersRunning);

            Task Timer;
            while (!Bot.ShutdownRequested)
            {
                Timer = Task.Delay(1);

                foreach (var Task in Handlers.Select(Handler => Handler.Tick()))
                {
                    try
                    {
                        await Task;
                    }
                    catch (Exception Ex)
                    {
                        Bot.Log(Ex);
                    }
                }
                
                await Timer;
            }

            Parallel.ForEach(Handlers, async Handler =>
            {
                try
                {
                    await Handler.Save();
                }
                catch { }
            });
        }

        internal async Task<int> Send(Destination Destination, string Text, bool Markdown = true, object State = null, int Id = 0, string ReplyId = null)
        {
            if (Id == 0)
            {
                Id = Interlocked.Increment(ref CurrentMessage);
            }

            await Bot.Clients[Destination.Token].Send(new SendMessage
            {
                Id = Id,
                Chat = Destination.Chat,
                Text = Text,
                Markdown = Markdown,
                State = State,
                ReplyId = ReplyId
            });

            return Id;
        }

        internal Task Edit(int Id, Destination Destination, string Text, bool Markdown = true, object State = null, string ReplyId = null)
        {
            return Bot.Clients[Destination.Token].Edit(new SendMessage
            {
                Id = Id,
                Chat = Destination.Chat,
                Text = Text,
                Markdown = Markdown,
                State = State,
                ReplyId = ReplyId
            });
        }

        internal Task Delete(int Id, Destination Destination)
        {
            return Bot.Clients[Destination.Token].Delete(new SendMessage
            {
                Id = Id,
                Chat = Destination.Chat,
                Text = null
            });
        }

        internal async Task Stream(Destination Destination, byte[] Audio)
        {
            if (Bot.Clients.ContainsKey(Destination.Token))
            {
                await Bot.Clients[Destination.Token].Stream(Destination.Chat, Audio);
            }
        }

        internal async Task<int> SendAll(string Text, bool Markdown = true, object State = null)
        {
            var Id = Interlocked.Increment(ref CurrentMessage);
            var Tasks = new Task[TextDestination.Length];
            for (var i = 0; i < TextDestination.Length; i++)
            {
                Tasks[i] = Send(TextDestination[i], Text, Markdown, State, Id);
            }

            Tasks.WaitAllAsync();
            return Id;
        }

        internal async Task EditAll(int Id, string Text, bool Markdown = true, object State = null)
        {
            var Tasks = new Task[TextDestination.Length];
            for (var i = 0; i < TextDestination.Length; i++)
            {
                Tasks[i] = Edit(Id, TextDestination[i], Text, Markdown, State);
            }

            await Tasks.WaitAllAsync();
        }

        internal async Task DeleteAll(int Id)
        {
            var Tasks = new Task[TextDestination.Length];
            for (var i = 0; i < TextDestination.Length; i++)
            {
                Tasks[i] = Delete(Id, TextDestination[i]);
            }

            await Tasks.WaitAllAsync();
        }

        internal async Task StreamAll(byte[] Audio)
        {
            try
            {
                var Tasks = new Task[AudioDestination.Length];
                for (var i = 0; i < AudioDestination.Length; i++)
                {
                    Tasks[i] = Stream(AudioDestination[i], Audio);
                }

                Tasks.WaitAllAsync();
            }
            catch (IndexOutOfRangeException)
            {
                //Update while sending
            }
        }

        internal async Task UpdateCache()
        {
            var TextDestinationList = new List<Destination>();
            var AudioDestinationList = new List<Destination>();

            using (var Context = Bot.GetDb)
            {
                FeedInfo = Context.Feed.Where(x => x.Id == Id + 1).FirstOrDefault();
                if (FeedInfo == null)
                {
                    FeedInfo = new Feed
                    {
                        Id = Id + 1
                    };

                    Context.Feed.Add(FeedInfo);
                    await Context.SaveChangesAsync();
                }

                foreach (var FeedLink in Context.DiscordFeedlink.Where(x => x.Feed == Id))
                {
                    TextDestinationList.Add(new Destination
                    {
                        Token = FeedLink.Token,
                        Chat = FeedLink.TextChannel
                    });

                    if (FeedLink.VoiceChannel != null)
                    {
                        AudioDestinationList.Add(new Destination
                        {
                            Token = FeedLink.Token,
                            Chat = FeedLink.VoiceChannel
                        });
                    }
                }

                foreach (var FeedLink in Context.TelegramFeedlink.Where(x => x.Feed == Id))
                {
                    TextDestinationList.Add(new Destination
                    {
                        Token = FeedLink.Token,
                        Chat = FeedLink.Chat.ToString()
                    });
                }
            }

            TextDestination = TextDestinationList.ToArray();
            AudioDestination = AudioDestinationList.ToArray();
        }
    }
}
