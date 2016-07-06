using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mirai.Commands
{
    class Audio
    {
        internal static async Task Add(ReceivedMessage Message)
        {
            var SongData = new SongData(Message.Text);
            if (SongData.Found)
            {
                var Place = await Message.Feed.Music.AddSong(Message.Text, SongData.Title);
                if (Place > 0)
                {
                    await Message.Respond($"`{SongData.Title}` has been added at #{Place}");
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
                Message.Respond($"Skipped {Message.Feed.Music.Playing.Song.Title}");
                Message.Feed.Music.Playing.Skip = true;
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
                    Files = SongData.LocalFiles(Search);
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
                        string Info = "";
                        for (int i = 0; i < Files.Length; i++)
                        {
                            Info += (i + 1) + ". `" + Files[i] + "`\n";
                        }

                        Message.Respond("Multiple files found\n" + Info);
                    }
                }
                else
                {
                    foreach (var File in ToAdd)
                    {
                        if (await Message.Feed.Music.AddSong(File, File, false) == 0)
                        {
                            ToAdd.Remove(File);
                        }
                    }
                    
                    if (ToAdd.Count > 0)
                    {
                        Message.Feed.Music.UpdateAll();
                        Message.Respond($"Added {ToAdd.Count} songs\n{string.Join("\n", ToAdd)}");
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

        internal static async Task Remove(ReceivedMessage Message)
        {
            var SongNames = await Message.Feed.Music.RemoveSongs(Message.Text.ParseInts());
            if (SongNames.Length > 0)
            {
                await Message.Respond($"**Removed**\n{string.Join("\n", SongNames)}");
            }
            else
            {
                await Message.Respond($"No songs were removed");
            }
        }
    }
}
