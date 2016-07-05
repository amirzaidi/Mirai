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
                Message.Feed.Music.AddSong(Message.Text, SongData.Title);
            }
            else
            {
                Message.Respond("That song can not be found");
            }
        }

        internal static async Task Skip(ReceivedMessage Message)
        {
            if (Message.Feed.Music.Playing != null)
            {
                Message.Feed.Music.Playing.Skip = true;
            }
        }

        internal static async Task Playing(ReceivedMessage Message)
        {
            await Message.Feed.Music.ResendUpdate(Message.Origin);
        }

        internal static async Task Playlist(ReceivedMessage Message)
        {
            await Message.Feed.Music.ResendUpdate(Message.Origin);
        }
    }
}
