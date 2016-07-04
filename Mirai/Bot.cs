using Mirai.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mirai
{
    class Bot
    {
        internal static Dictionary<string, IClient> Clients = new Dictionary<string, IClient>();
        internal static Semaphore Waiter = new Semaphore(0, int.MaxValue);

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
            Waiter.WaitOne();

            int i = 0;
            var Disconnecting = new Task[Clients.Count];
            foreach (var Conn in Clients.Values)
            {
                Disconnecting[i++] = Conn.Disconnect();
            }

            Task.WaitAll(Disconnecting);
        }

        static async void LoadAsync()
        {
            using (var Context = GetDb)
            {
                foreach (var Cred in from Rows in Context.DiscordCredentials select Rows)
                {
                    Clients.Add(Cred.Token, new Client.Discord(Cred.App, Cred.Token));
                }

                foreach (var Cred in from Rows in Context.TelegramCredentials select Rows)
                {
                    Clients.Add(Cred.Token, new Client.Telegram(Cred.Token));
                }
            }

            foreach (var Conn in Clients.Values)
            {
                await Conn.Connect();
                var Info = await Conn.Info();

                Log($"Connected to {Info.Type} as {Info.Name}");
            }

            await Task.Delay(5000);
            Waiter.Release();
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
