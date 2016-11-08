using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mirai.Commands
{
    class Audio
    {
        internal static async Task Add(ReceivedMessage Message)
        {
            var SongDatas = SongData.Search(Message.Text);
            if (SongDatas.Count != 0)
            {
                var Place = await Message.Feed.Music.AddSong(SongDatas[0]);
                if (Place > 0)
                {
                    await Message.Respond($"`{SongDatas[0].Title}` has been added at #{Place}");
                }
            }
            else
            {
                await Message.Respond("That song can not be found");
            }
        }

        internal static async Task Skip(ReceivedMessage Message)
        {
            if (Message.Feed.Music.Playing != null)
            {
                Message.Respond($"Skipped `{Message.Feed.Music.Playing?.Song.Title}`");
                //Message.Feed.Music.Playing?.Skip.Cancel();
                Message.Feed.Music.Playing?.Stop();
            }
            else
            {
                Message.Respond($"Currently, no song is playing");
            }
        }

        internal static async Task Playing(ReceivedMessage Message)
        {
            await Message.Feed.Music.ResendUpdate(Message.Origin);
        }

        private static string[] Files = null;
        internal static async Task Local(ReceivedMessage Message)
        {
            var Search = Message.Text.ToLower().Trim();
            if (Search != string.Empty)
            {
                var ToAdd = new List<string>();
                if (Files != null)
                {
                    if (Search == "all")
                    {
                        ToAdd = Files.ToList();
                    }
                    else
                    {
                        foreach (var Num in Search.ParseInts())
                        {
                            ToAdd.Add(Files[Num - 1]);
                        }
                    }

                    Files = null;
                }

                if (ToAdd.Count == 0)
                {
                    Files = new System.IO.DirectoryInfo(SongData.MusicDir).GetFiles()
                        .Where(x => x.Name.Length >= Message.Text.Length && x.Name.ToLower().Contains(Message.Text.ToLower()) && !x.Attributes.HasFlag(System.IO.FileAttributes.System))
                        .OrderBy(x => x.Name)
                        .Select(x => x.Name)
                        .ToArray();

                    if (Files.Length == 0)
                    {
                        Message.Respond("No files were found");
                    }
                    else if (Files.Length == 1)
                    {
                        Message.Text = Files[0];
                        Add(Message);
                    }
                    else
                    {
                        var Info = string.Empty;
                        for (int i = 0; i < Files.Length; i++)
                        {
                            Info += (i + 1) + ". `" + Files[i] + "`\n";
                        }

                        Message.Respond("Multiple files found\n" + Info);
                    }
                }
                else
                {
                    var Added = new List<string>();
                    foreach (var File in ToAdd.ToArray())
                    {
                        var Place = await Message.Feed.Music.AddSong(new SongData
                        {
                            FullName = File,
                            Url = SongData.MusicDir + File,
                            Type = SongType.Local
                        }, false);
                        if (Place != 0)
                        {
                            Added.Add($"{Place}. `{File}`");
                        }
                    }
                    
                    if (Added.Count > 0)
                    {
                        Message.Feed.Music.UpdateAll();
                        Message.Respond($"Added {Added.Count} songs\n{string.Join("\n", Added)}");
                    }
                }
            }
        }

        internal static async Task Push(ReceivedMessage Message)
        {
            var Split = Message.Text.Split(' ');

            int Place, ToPlace = 1;
            if (int.TryParse(Split[0], out Place))
            {
                if (Split.Length == 3)
                {
                    int.TryParse(Split[2], out ToPlace);
                }

                var Pushed = Message.Feed.Music.Push(Place, ToPlace);
                if (Pushed != null)
                {
                    Message.Respond($"Pushed `{Pushed}` to #{ToPlace}");
                    return;
                }
            }
        }

        internal static async Task Repeat(ReceivedMessage Message)
        {
            int Count = 1;
            if (Message.Text != string.Empty)
            {
                int.TryParse(Message.Text, out Count);
            }

            var PlayingTitle = Message.Feed.Music.Repeat(ref Count);
            if (PlayingTitle != null)
            {
                Message.Respond($"Repeated `{PlayingTitle}` {Count} times");
            }
            else
            {
                Message.Respond($"Currently, no song is playing");
            }
        }

        internal static async Task Restart(ReceivedMessage Message)
        {
            int Count = 1;
            var PlayingTitle = Message.Feed.Music.Repeat(ref Count);
            if (PlayingTitle != null)
            {
                Message.Respond($"Restarted `{PlayingTitle}`");
                Message.Feed.Music.Playing?.Stop();
            }
        }

        internal static async Task Clear(ReceivedMessage Message)
        {
            Message.Feed.Music.Queue = new ConcurrentQueue<SongData>();
            Message.Respond($"The queue has been cleared");
            Message.Feed.Music.UpdateAll();
        }

        internal static async Task Shuffle(ReceivedMessage Message)
        {
            var Rand = new Random();
            Message.Feed.Music.Queue = new ConcurrentQueue<SongData>(Message.Feed.Music.Queue.OrderBy(x => Rand.Next()));
            Message.Respond($"The queue has been shuffled");
            Message.Feed.Music.UpdateAll();
        }

        internal static async Task Filter(ReceivedMessage Message)
        {
            Message.Feed.Music.Filter = Message.Text;
            Message.Respond($"The filter has been set");
        }

        internal static async Task Remove(ReceivedMessage Message)
        {
            var SongNames = await Message.Feed.Music.RemoveSongs(Message.Text.ParseInts());
            if (SongNames.Length > 0)
            {
                await Message.Respond($"**Removed**\n{string.Join("\n", SongNames.Select(x => $"`{x}`"))}");
            }
            else
            {
                await Message.Respond($"No songs were removed");
            }
        }
    }
}
