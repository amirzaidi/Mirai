using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Mirai.Client;
using Mirai.Database.Tables;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mirai
{
    class Bot
    {
        internal static FeedContext[] Feeds = new FeedContext[10];
        internal static ConcurrentDictionary<string, IClient> Clients = new ConcurrentDictionary<string, IClient>();
        internal const string Command = "/";
        internal const string JoinFeed = "joinfeed";
        internal const string LeaveFeed = "leavefeed";
        internal static bool ShutdownRequested = false;
        internal static string ShutdownCode;
        internal static Dictionary<string, string> Config = new Dictionary<string, string>();

        internal static Database.MiraiContext GetDb
        {
            get
            {
                return new Database.MiraiContext();
            }
        }

        static void Main(string[] args)
        {
            LoadAsync();

            var Start = DateTime.Now;
            while (!ShutdownRequested)
            {
                Thread.Sleep(50);
                Console.Title = $"[{(DateTime.Now - Start).ToString("%d")} days, {(DateTime.Now - Start).ToString(@"%h\:mm\:ss")}] Mirai 3.0 - {FeedContext.HandlersRunning} Feeds Running - Shutdown Code {ShutdownCode}";
            }

            Log("Shutdown requested, shutting down ASAP");
            
            Task.WaitAll(Feeds.Select(x => x.HandleTask).ToArray());
            Task.WaitAll(Clients.Values.Select(x => x.Disconnect()).ToArray());
        }

        static async void LoadAsync()
        {
            MusicProcessor.Buffers = new BufferPool(1920 * 2, (int)Math.Pow(2, 16));

            using (var Context = GetDb)
            {
                foreach (var KVP in from Rows in Context.Config select Rows)
                {
                    Config[KVP.Name] = KVP.Value;
                }

                SongData.YT = new YouTubeService(new BaseClientService.Initializer
                {
                    ApiKey = Config["Google"]
                });

                ;
                
                for (int i = 0; i < Feeds.Length; i++)
                {
                    Feeds[i] = new FeedContext(i);
                }

                foreach (var Cred in from Rows in Context.DiscordCredentials select Rows)
                {
                    Clients.TryAdd(Cred.Token, new Client.Discord(Cred.App, Cred.Token, Cred.Owner));
                }

                foreach (var Cred in from Rows in Context.TelegramCredentials select Rows)
                {
                    Clients.TryAdd(Cred.Token, new Client.Telegram(Cred.Token, Cred.Owner));
                }
            }

            Commands.Parser.LoadCommands();

            foreach (var Conn in Clients.Values)
            {
                await Conn.Connect();
                var Info = await Conn.Info();

                Log($"Connected to {Info.Type} as {Info.Name}");
            }

            UpdateCache();

            ShutdownCode = new Random().Next(0, 9999).ToString().PadLeft(4, '0');
            Log($"Fully functional - Shutdown code is {ShutdownCode}");
        }

        internal static void UpdateCache()
        {
            lock (Feeds)
            {
                foreach (var Client in Clients.Values)
                {
                    Client.UpdateCache();
                }

                foreach (var Feed in Feeds)
                {
                    Feed.UpdateCache();
                }
            }
        }

        internal static void Log(string Text)
        {
            Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}][{Thread.CurrentThread.ManagedThreadId}] {Text}");
        }

        internal static void Log(object Obj)
        {
            Log(Obj.ToString());
        }
    }
}
