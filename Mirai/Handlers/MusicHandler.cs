using Mirai.Database.Tables;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mirai.Handlers
{
    struct TitledQuery
    {
        internal string Query;
        internal string Title;
    }

    class MusicHandler : IHandler
    {
        private FeedContext Feed;
        internal const int MaxQueued = 25;
        internal ConcurrentQueue<TitledQuery> Queue = new ConcurrentQueue<TitledQuery>();
        internal MusicProcessor Playing;

        private byte[] CurrentSend = null;
        private byte[] NextSend = null;
        private Queue<Task> Sending = new Queue<Task>();

        private string PlayingMessage
        {
            get
            {
                var Text = new StringBuilder();

                if (Playing == null)
                {
                    Text.Append("Nothing is playing");
                }
                else
                {
                    Text.Append("Playing `");
                    Text.Append(Playing.Song.Title);
                    Text.Append("`");
                }

                Text.Append("\n");

                var Songs = Queue.ToArray();

                Text.Append(Songs.Length);
                Text.Append(" Song(s) Queued\n");

                int Count = 0;
                foreach (var Entry in Songs)
                {
                    Text.Append(++Count);
                    Text.Append(". ");
                    Text.Append(Entry.Title);
                    Text.Append("\n");
                }

                return Text.ToString();
            }
        }

        internal int PlayingMessageId
        {
            get;
            private set;
        }

        internal MusicHandler(FeedContext Feed)
        {
            this.Feed = Feed;
            PlayingMessageId = 0;

            Task.Run(delegate
            {
                string[] Queries;
                using (var Context = Bot.GetDb)
                {
                    Queries = Context.Song.Where(x => x.Feed == Feed.Id).OrderBy(x => x.Place).Select(x => x.Query).ToArray();
                }

                Queue = new ConcurrentQueue<TitledQuery>(Queries.Select(Query => new TitledQuery
                {
                    Query = Query,
                    Title = new SongData(Query).Title
                }));
            });
        }

        internal async Task UpdateAll()
        {
            if (PlayingMessageId == 0)
            {
                PlayingMessageId = await Feed.SendAll(PlayingMessage);
            }
            else
            {
                await Feed.EditAll(PlayingMessageId, PlayingMessage);
            }
        }

        internal async Task ResendUpdate(Destination Destination)
        {
            if (PlayingMessageId == 0)
            {
                await UpdateAll();
            }
            else
            {
                Feed.Delete(PlayingMessageId, Destination);
                Feed.Send(Destination, PlayingMessage, Id: PlayingMessageId);
            }
        }

        public async Task Tick()
        {
            if (Feed.AudioDestination.Length != 0)
            {
                if (Playing != null && Playing.Skip)
                {
                    Playing.Dispose();
                    Playing = null;
                    await UpdateAll();
                    await Task.Delay(50);
                }

                if (Playing == null)
                {
                    //Dequeue a song
                    TitledQuery TitledQuery;
                    if (Queue.TryDequeue(out TitledQuery))
                    {
                        Playing = new MusicProcessor(new SongData(TitledQuery.Query));
                        await UpdateAll();
                    }
                }
                else
                {
                    if (NextSend != null)
                    {
                        while (Sending.Count > 2)
                        {
                            await Sending.Dequeue();
                        }

                        Sending.Enqueue(Feed.StreamAll(NextSend));
                        if (CurrentSend != null)
                        {
                            MusicProcessor.Buffers.Return(CurrentSend);
                        }

                        CurrentSend = NextSend;
                        NextSend = null;
                    }

                    if (Playing.QueuedBuffers.Count > 0)
                    {
                        NextSend = Playing.QueuedBuffers.Dequeue();
                        Playing.Waiter.Release(1);
                    }
                    else if (Playing.FinishedBuffer)
                    {
                        Playing.Skip = true;
                    }
                }
            }
        }

        internal async Task<int> AddSong(string Query, string Title, bool Update = true)
        {
            if (Queue.Count < MaxQueued)
            {
                Queue.Enqueue(new TitledQuery
                {
                    Query = Query,
                    Title = Title
                });

                if (Update)
                {
                    UpdateAll();
                }

                return Queue.Count;
            }

            return 0;
        }

        internal string Push(int Place, int ToPlace)
        {
            var NewQueue = new ConcurrentQueue<TitledQuery>();
            var Songs = Queue.ToList();
            if (Place > 0 && ToPlace > 0 && Place != ToPlace && Songs.Count >= Place && Songs.Count >= ToPlace)
            {
                var Pushed = Songs[Place - 1];
                Songs.Remove(Pushed);
                Songs.Insert(ToPlace - 1, Pushed);

                foreach (var Song in Songs)
                {
                    NewQueue.Enqueue(Song);
                }

                Queue = NewQueue;
                UpdateAll();

                return Pushed.Title;
            }

            return null;
        }

        internal string Repeat(ref int Count)
        {
            if (Playing != null)
            {
                var Songs = Queue.ToArray();
                if (Count + Songs.Length > MaxQueued)
                {
                    Count = MaxQueued - Songs.Length;
                }

                var NewQueue = new ConcurrentQueue<TitledQuery>();

                for (int i = 0; i < Count; i++)
                {
                    NewQueue.Enqueue(new TitledQuery
                    {
                        Query = Playing.Song.Query,
                        Title = Playing.Song.Title
                    });
                }

                foreach (var Song in Queue)
                {
                    NewQueue.Enqueue(Song);
                }

                Queue = NewQueue;
                UpdateAll();

                return Playing.Song.Title;
            }

            return null;
        }

        internal async Task<string[]> RemoveSongs(int[] Places)
        {
            var Removed = new List<string>();
            int i = 0;

            var NewQueue = new ConcurrentQueue<TitledQuery>();
            foreach (var Song in Queue.ToArray())
            {
                if (Places.Contains(++i))
                {
                    Removed.Add($"{i}. {Song.Title}");
                }
                else
                {
                    NewQueue.Enqueue(Song);
                }
            }

            if (Removed.Count > 0)
            {
                Queue = NewQueue;
                UpdateAll();
            }

            return Removed.ToArray();
        }

        public async Task Save()
        {
            using (var Context = Bot.GetDb)
            {
                Context.Song.RemoveRange(Context.Song.Where(x => x.Feed == Feed.Id));

                var i = 1;
                var Songs = Queue.Select(x => new Song
                {
                    Feed = Feed.Id,
                    Place = i++,
                    Query = x.Query
                }).ToList();

                if (Playing != null)
                {
                    Songs.Insert(0, new Song
                    {
                        Feed = Feed.Id,
                        Place = 0,
                        Query = Playing.Song.Query
                    });
                }

                Context.Song.AddRange(Songs);
                await Context.SaveChangesAsync();
            }
        }
    }
}
