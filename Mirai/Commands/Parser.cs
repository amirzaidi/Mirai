using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mirai.Commands
{
    class Parser
    {
        private static Dictionary<string, Command[]> Categories = new Dictionary<string, Command[]>();

        internal static async Task Parse(ReceivedMessage Message)
        {
            if (Message.Command == "shutdown" && Message.Text == Bot.ShutdownCode)
            {
                Bot.ShutdownRequested = true;
                return;
            }

            var FullText = $"/{Message.Command} {Message.Text}".ToLower();
            foreach (var Category in Categories)
            {
                if (Category.Key == string.Empty || (Message.Feed.FeedInfo?.IsEnabled(Category.Key) ?? false))
                {
                    if (Message.Command != null)
                    {
                        Message.Command = Message.Command.ToLower();
                        foreach (var Command in Category.Value)
                        {
                            if (Command.Prefix == CommandType.Command && Command.Keys.Contains(Message.Command))
                            {
                                await Command.Handler(Message);
                                return;
                            }
                        }
                    }

                    foreach (var Command in Category.Value)
                    {
                        if (Command.Prefix == CommandType.Text && Command.Keys.All(Key => FullText.Contains(Key)))
                        {
                            Message.Text = FullText;
                            await Command.Handler(Message);
                            return;
                        }
                    }
                }
            }
        }

        internal static async Task Help(ReceivedMessage Message)
        {
            var Disabled = new List<string>();
            var UserRank = Message.SenderRank();

            foreach (var Category in Categories)
            {
                if (Category.Key == "" || Message.Feed.FeedInfo.IsEnabled(Category.Key))
                {
                    var CategoryCommands = string.Empty;

                    foreach (var Command in Category.Value)
                    {
                        var MinRank = Command.MinRank(Message.Feed.Id);

                        if (UserRank == byte.MaxValue)
                        {
                            CategoryCommands += $"({MinRank}) ";
                        }
                        else if (UserRank < MinRank)
                        {
                            continue;
                        }

                        if (Command.Prefix == CommandType.Command)
                        {
                            CategoryCommands += $"{string.Join(" or ", Command.Keys)} ~ `{Command.Description}`\n";
                        }
                        else
                        {
                            CategoryCommands += $"{string.Join(" _and_ ", Command.Keys)}\n";
                        }
                    }

                    if (CategoryCommands != string.Empty)
                    {
                        await Message.Respond($"{(Category.Key == string.Empty ? "**Main**" : "**" + Category.Key + "**")}\n{CategoryCommands}");
                        await Task.Delay(100);
                    }
                }
                else
                {
                    Disabled.Add($"**{Category.Key}**");
                }
            }

            if (Disabled.Count > 0)
            {
                await Message.Respond($"**Disabled**\n{string.Join(", ", Disabled)}");
            }
        }

        internal static async Task Toggle(ReceivedMessage Message)
        {
            var Response = $"category of commands: `{ Message.Text}`";
            if (await Message.Feed.FeedInfo.Toggle(Message.Text))
            {
                await Message.Feed.SendAll($"Enabled {Response}");
            }
            else
            {
                await Message.Feed.SendAll($"Disabled {Response}");
            }
        }

        internal static void LoadCommands()
        {
            Categories.Add(string.Empty, new[] {
                new Command(CommandType.Command, new [] { "help", "commands" }, "Shows all your commands", Help),
                new Command(CommandType.Command, new [] { "toggle", "togglecat" }, "Turns a category on/off", Toggle)
            });

            Categories.Add(typeof(Administration).Name, new [] {
                new Command(CommandType.Command, "minrank", "Sets a necessary rank to use a command", Administration.MinRank),
                new Command(CommandType.Command, new [] { "giverank", "setrank" }, "Sets someone's rank", Administration.GiveRank),
                new Command(CommandType.Command, new [] { "rank", "myrank" }, "See your rank in this server", Administration.Rank),
                new Command(CommandType.Command, new [] { "id", "myid" }, "See your personal id", Administration.Id),
                //new Command(CommandType.Command, "eval", "Runs a script", Administration.Eval)
            });

            Categories.Add(typeof(Audio).Name, new[] {
                new Command(CommandType.Command, new [] { "add", "q" }, "Adds a song title to the music queue", Audio.Add),
                new Command(CommandType.Command, new [] { "local", "addlocal", "l" }, "Adds a local song title to the music queue", Audio.Local),
                new Command(CommandType.Command, new [] { "push", "p" }, "Pushes a song to the top of the music queue", Audio.Push),
                new Command(CommandType.Command, new [] { "repeat" }, "Repeats the currently playing song", Audio.Repeat),
                new Command(CommandType.Command, new [] { "remove", "r" }, "Removes a song from the music queue", Audio.Remove),
                new Command(CommandType.Command, new [] { "playing", "song", "np", "playlist", "lq", "queue" }, "Shows the current song and playlist", Audio.Playing),
                new Command(CommandType.Command, new [] { "skip", "next", "n" }, "Skips the current song", Audio.Skip),
                new Command(CommandType.Command, new [] { "shuffle", "s" }, "Shuffles the current queue", Audio.Shuffle),
                new Command(CommandType.Command, "clear", "Clears the current queue", Audio.Clear)
            });

            Categories.Add(typeof(Trivia).Name, new [] {
                new Command(CommandType.Command, new [] { "starttrivia", "t" }, "Starts a trivia match in your channel", Trivia.Start),
                new Command(CommandType.Text, new [] { "." }, "Try to answer the trivia question", Trivia.Answer),
                new Command(CommandType.Command, new [] { "points", "leaderboard", "tl" }, "Shows the current trivia's leaderboard", Trivia.Points),
                new Command(CommandType.Command, new [] { "stoptrivia", "tq" }, "Stops a trivia match in your channel", Trivia.Stop)
            });

            Categories.Add(typeof(Search).Name, new [] {
                /*new Command(CommandType.Command, new string[] { "ask", "8ball", "answer" }, "Ask me a question", Search.Ask),
                new Command(CommandType.Command, new string[] { "youtube", "yt" }, "Searches for a youtube video", Search.Youtube),
                new Command(CommandType.Command, new string[] { "image", "img" }, "Search for an image", Search.Image),*/
                new Command(CommandType.Command, "osu", "Show someone's osu stats", Search.Osu),
                //new Command(CommandType.Command, new string[] { "avatar", "av" }, "Show someone's avatar", Search.Avatar),
                new Command(CommandType.Command, new [] { "define", "ud", "what's", "whats", "who's", "whos" }, "Search for a term", Search.Define),
                new Command(CommandType.Command, "anime", "Search for an anime - shorthand {name}", Search.Anime),
                new Command(CommandType.Command, "manga", "Search for a manga - shorthand <name>", Search.Manga),
                new Command(CommandType.Text, new [] { "{", "}" }, "Search for an anime", Search.AnimeShort),
                new Command(CommandType.Text, new [] { "<", ">" }, "Search for a manga", Search.MangaShort)
            });

            Categories.Add(typeof(Lewd).Name, new [] {
                new Command(CommandType.Command, new [] { "lewd", "booru", "nsfw" }, "Search for a lewd image", Lewd.Search)
            });

            string Spam = "O - oooooooooo AAAAE - A - A - I - A - U - JO - oooooooooooo AAE - O - A - A - U - U - A - E - eee - ee - eee AAAAE - A - E - I - E - A - JO - ooo - oo - oo - oo EEEEO - A - AAA - AAAA";
            string FullSpam = "";

            while (FullSpam.Length + Spam.Length < 1992)
            {
                FullSpam += Spam + "\n";
            }

            Categories.Add(typeof(Conversation).Name, new[] {
                new Command(CommandType.Command, new [] { "hi", "hey", "hello" }, "Say hello to me", "Hi!"),
                new Command(CommandType.Command, "choose", "Choose from a list", Conversation.Choose),
                /*new Command(CommandType.Command, "how are you", "Check if my owner is online", Conversation.Status),
                new Command(CommandType.Command, new string[] { "do you like me", "do you love me" }, "...", Conversation.Love),*/
                new Command(CommandType.Command, new string[] { "insult", "flame" }, "Insult a person", Conversation.Insult),
                //new Command(CommandType.Command, new string[] { "praise", "compliment" }, "Praise a person", Conversation.Praise),
                new Command(CommandType.Command, new string[] { "attack", "stab" }, "Attack a person", Conversation.Stab),
                /*new Command(CommandType.Command, new string[] { "welcome", "say hi to" }, "Welcome someone", Conversation.Hi),
                new Command(CommandType.Command, "say bye to", "Say bye to someone", Conversation.Bye),
                new Command(CommandType.Command, new string[] { "go out with me", "will you go out with me" }, "...", Conversation.GoOut),
                new Command(CommandType.Command, new string[] { "you're best girl", "you are best girl" }, "Compliment me", Conversation.Best),
                new Command(CommandType.Command, new string[] { "you're not best girl", "you are not best girl", "cry" }, "Don't be heartless", Conversation.Cry),*/
                new Command(CommandType.Command, new [] { "here", "reward" }, "Give me a reward", Conversation.Reward),
                //new Command(CommandType.Command, new string[] { "no bully", "stop bully" }, "Tell me to enforce a no bully zone", Conversation.NoBully),
                new Command(CommandType.Command, "sing", "Ask me to sing", Conversation.Sing),
                new Command(CommandType.Command, "dance", "Ask me to dance", Conversation.Dance),
                new Command(CommandType.Command, new [] { "meme", "dank", "dankmeme", "animeme" }, "Get a dank meme", Conversation.Meme),

                new Command(CommandType.Text, "megane", "", "Fuyukai desu!"),
                new Command(CommandType.Text, "burn the chat", "", "🔥 ด้้้้้็็็็็้้้้้็็็็็้้้้้้้้็็็็็้้้้้็็ด้้้้้็็็็็้้้้้็็็็็้้้้้้้้็็็็็้้้้้็็็็็้้้้้้้้็็็ด้้้้้็็็็็้้้้้็็็็็้้้้้้้้็็็็็้้้้้็็็็็้้้้ 🔥"),
                new Command(CommandType.Text, "kuriyama?", "", "Yes?"),
                new Command(CommandType.Text, "mirai?", "", "Please call me Kuriyama"),
                //new Command(CommandType.None, new [] { "fuck mirai", "fuck you mirai" }, "", Conversation.Cry),

                new Command(CommandType.Text, new [] { "aaaae", "o-ooo" }, "", "**" + FullSpam + "**"),
                new Command(CommandType.Text, "fap", "", "ಠ.ಠ"),
                new Command(CommandType.Text, "fuyukai desu", "", "That's my joke!"),
                new Command(CommandType.Text, "\\o\\", "", "/o/"),
                new Command(CommandType.Text, "/o/", "", "\\o\\"),
                new Command(CommandType.Text, "\\o/", "", "\\o/"),
                new Command(CommandType.Text, "/lenny", "", "( ͡° ͜ʖ ͡°)"),
                //new Command(CommandType.None, "$$$", "", Trivia.Points)
            });
        }

        internal static bool ValidKey(string Key)
        {
            return Categories.SelectMany(x => x.Value).Any(x => x.Keys.Contains(Key));
        }
    }
}
