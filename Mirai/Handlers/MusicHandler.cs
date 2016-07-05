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
        private const int MaxQueued = 15;
        internal ConcurrentQueue<TitledQuery> Queue;
        internal MusicProcessor Playing;

        private byte[] CurrentSend = null;
        private byte[] NextSend = null;
        private Queue<Task> Sending = new Queue<Task>(3);

        private string PlayingMessage
        {
            get
            {
                var Text = new StringBuilder();

                Text.Append("Playing ");
                Text.Append(Playing.Song.Title);
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

        public MusicHandler(FeedContext Feed)
        {
            this.Feed = Feed;
            PlayingMessageId = 0;

            Task.Run(delegate
            {
                string[] Queries;
                using (var Context = Bot.GetDb)
                {
                    var QuerySets = from Rows in Context.Song
                                  where Rows.Feed == Feed.Id
                                  orderby Rows.Place ascending
                                  select Rows.Query;

                    Queries = QuerySets.ToArray();
                }

                Queue = new ConcurrentQueue<TitledQuery>(Queries.Select(Query => new TitledQuery
                {
                    Query = Query,
                    Title = new SongData(Query).Title
                }));
            });
        }

        public async Task UpdateAll()
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

        public async Task ResendUpdate(Destination Destination)
        {
            if (PlayingMessageId == 0)
            {
                await UpdateAll();
            }
            else
            {
                Feed.Delete(PlayingMessageId, Destination);
                Feed.Send(Destination, PlayingMessage, PlayingMessageId);
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
                        if (Sending.Count == 3)
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

        internal async Task AddSong(string Query, string Title)
        {
            if (Queue.Count < MaxQueued)
            {
                Queue.Enqueue(new TitledQuery
                {
                    Query = Query,
                    Title = Title
                });

                await Feed.SendAll($"Added {Title}");
            }
        }

        public async Task Save()
        {
            //Serialize playlist
        }
    }
}
