using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mirai.Commands
{
    class Parser
    {
        private static Dictionary<string, Command[]> Categories = new Dictionary<string, Command[]>();

        internal static async void Parse(ReceivedMessage Message)
        {
            if (Message.Command == "shutdown" && Message.Text == Bot.ShutdownCode)
            {
                Bot.ShutdownRequested = true;
            }
            else
            {
                foreach (var Command in Categories.SelectMany(x => x.Value))
                {
                    if (Command.Prefix == Command.PrefixType.Command)
                    {
                        if (Message.Command != null && Command.Keys.Contains(Message.Command.ToLower()))
                        {
                            Command.Handler(Message);
                            return;
                        }
                    }
                    else if (Command.Keys.Any(x => Message.Text.ToLower().Contains(x)))
                    {
                        Command.Handler(Message);
                        return;
                    }
                }
            }
        }

        internal static void LoadCommands()
        {
            Categories.Add(string.Empty, new [] {
                new Command(Command.PrefixType.Command, new [] { "help", "commands" }, "Shows all your commands", Help),
                //new Command(Command.PrefixType.Command, new string[] { "toggle", "togglecat" }, "Turns a category on/off", ToggleCategory)
            });

            /*Categories.Add(typeof(Administration).Name, new [] {
                new Command(Command.PrefixType.Command, "minrank", "Sets a necessary rank to use a command", Administration.Permission),
                new Command(Command.PrefixType.Command, new [] { "giverank", "setrank" }, "Sets someone's rank", Administration.Rank),
                new Command(Command.PrefixType.Command, "ranks", "See all special ranks in this server", Administration.Ranks),

                new Command(Command.PrefixType.Command, new [] { "sleep", "shutdown" }, "Shuts me down", Administration.Sleep),
                new Command(Command.PrefixType.Command, "setname", "Changes my name", Administration.SetName),
                new Command(Command.PrefixType.Command, "setavatar", "Changes my avatar", Administration.SetAvatar),
                new Command(Command.PrefixType.Command, "prune", "Removes some message history", Administration.Prune),
                new Command(Command.PrefixType.Command, "fix", "Clears the message queue", Administration.Fix),
                new Command(Command.PrefixType.Command, "eval", "Runs a script", Administration.Eval),
                new Command(Command.PrefixType.Command, "joinserver", "Sends the invite link to add me", Administration.JoinServer),
                new Command(Command.PrefixType.Command, "leaveserver", "Leaves this server, add my mention to confirm", Administration.LeaveServer)
            });*/

            Categories.Add(typeof(Audio).Name, new [] {
                //new Command(Command.PrefixType.Command, "join", "Joins your current voice channel", Audio.Join),
                //new Command(Command.PrefixType.Command, "leave", "Leaves any voice channel", Audio.Leave),
                new Command(Command.PrefixType.Command, new string[] { "add", "q" }, "Adds a song title to the music queue", Audio.Add),
                /*new Command(Command.PrefixType.Command, new string[] { "local", "addlocal", "l" }, "Adds a local song title to the music queue", Audio.Local),
                new Command(Command.PrefixType.Command, new string[] { "push", "p" }, "Pushes a song to the top of the music queue", Audio.Push),
                new Command(Command.PrefixType.Command, new string[] { "repeat" }, "Repeats the currently playing song", Audio.Repeat),
                new Command(Command.PrefixType.Command, new string[] { "remove", "r" }, "Removes a song from the music queue", Audio.Remove),
                new Command(Command.PrefixType.Command, new string[] { "volume", "vol" }, "Changes the volume of the music player", Audio.Volume),*/
                new Command(Command.PrefixType.Command, new string[] { "playing", "song", "np", "playlist", "lq", "queue" }, "Shows the current song and playlist", Audio.Playing),
                //new Command(Command.PrefixType.Command, new string[] {  }, "Lists the current playlist", Audio.Playlist),
                new Command(Command.PrefixType.Command, new string[] { "skip", "next", "n" }, "Skips the current song", Audio.Skip),
                /*new Command(Command.PrefixType.Command, new string[] { "shuffle", "s" }, "Shuffles the current queue", Audio.Shuffle),
                new Command(Command.PrefixType.Command, "clear", "Clears the current queue", Audio.Clear),
                new Command(Command.PrefixType.Command, "save", "Saves the current playlist", Audio.Save),
                new Command(Command.PrefixType.Command, "load", "Loads the current playlist from", Audio.Load),
                new Command(Command.PrefixType.Command, "tgpair", "Pairs a Telegram channel to a Discord channel", Audio.Pair),
                new Command(Command.PrefixType.Command, "tgunpair", "Unpairs all Telegram channels", Audio.Unpair),
                new Command(Command.PrefixType.Command, "tgtoggle", "Allows or disallows someone from using Telegram commands", Audio.TgToggle),
                new Command(Command.PrefixType.Command, "adhd", "Toggles ADHD", Audio.Adhd)*/
            });

            /*Categories.Add(typeof(Trivia).Name, new [] {
                new Command(Command.PrefixType.Command, new string[] { "starttrivia", "t" }, "Starts a trivia match in your channel", Trivia.Start),
                new Command(Command.PrefixType.Command, new string[] { "leaderboard", "tl" }, "Shows the current trivia's leaderboard", Trivia.Leaderboards),
                new Command(Command.PrefixType.Command, new string[] { "stoptrivia", "tq" }, "Stops a trivia match in your channel", Trivia.Stop)
            });*/

            /*Categories.Add(typeof(Search).Name, new [] {
                new Command(Command.PrefixType.Command, new string[] { "ask", "8ball" }, "Ask me a question", Search.Ask),
                new Command(Command.PrefixType.Command, new string[] { "youtube", "yt" }, "Searches for a youtube video", Search.Youtube),
                new Command(Command.PrefixType.Command, new string[] { "image", "img" }, "Search for an image", Search.Image),
                new Command(Command.PrefixType.Command, "osu", "Show someone's osu stats", Search.Osu),
                new Command(Command.PrefixType.Command, new string[] { "avatar", "av" }, "Show someone's avatar", Search.Avatar),
                new Command(Command.PrefixType.Command, new string[] { "define", "ud" }, "Search for a term", Search.Define),
                new Command(Command.PrefixType.Command, "anime", "Search for an anime - shorthand {name}", Search.AnimeInfo),
                new Command(Command.PrefixType.Command, "manga", "Search for a manga - shorthand <name>", Search.MangaInfo)
            });*/

            /*Categories.Add(typeof(Lewd).Name, new [] {
                new Command(Command.PrefixType.Command, new string[] { "lewd", "booru", "nsfw" }, "Search for a lewd image", Lewd.RandomLewd)
            });*/

            string Spam = "O - oooooooooo AAAAE - A - A - I - A - U - JO - oooooooooooo AAE - O - A - A - U - U - A - E - eee - ee - eee AAAAE - A - E - I - E - A - JO - ooo - oo - oo - oo EEEEO - A - AAA - AAAA";
            string FullSpam = "";

            while (FullSpam.Length + Spam.Length < 1992)
            {
                FullSpam += Spam + "\n";
            }

            Categories.Add(typeof(Conversation).Name, new [] {
                new Command(Command.PrefixType.Command, new [] { "hi", "hey", "hello" }, "Say hello to me", "Hi!"),
                /*new Command(Command.PrefixType.Command, "choose from", "Choose from a list", Conversation.Choose),
                new Command(Command.PrefixType.Command, "how are you", "Check if my owner is online", Conversation.Status),
                new Command(Command.PrefixType.Command, new string[] { "do you like me", "do you love me" }, "...", Conversation.Love),
                new Command(Command.PrefixType.Command, new string[] { "insult", "hate on" }, "Insult a person", Conversation.Insult),
                new Command(Command.PrefixType.Command, new string[] { "praise", "compliment" }, "Praise a person", Conversation.Praise),
                new Command(Command.PrefixType.Command, new string[] { "attack", "stab" }, "Stab a person", Conversation.Stab),
                new Command(Command.PrefixType.Command, new string[] { "welcome", "say hi to" }, "Welcome someone", Conversation.Hi),
                new Command(Command.PrefixType.Command, "say bye to", "Say bye to someone", Conversation.Bye),
                new Command(Command.PrefixType.Command, new string[] { "go out with me", "will you go out with me" }, "...", Conversation.GoOut),
                new Command(Command.PrefixType.Command, new string[] { "you're best girl", "you are best girl" }, "Compliment me", Conversation.Best),
                new Command(Command.PrefixType.Command, new string[] { "you're not best girl", "you are not best girl", "cry" }, "Don't be heartless", Conversation.Cry),
                new Command(Command.PrefixType.Command, new string[] { "take it", "take this", "here" }, "Give me a reward", Conversation.TakeIt),
                new Command(Command.PrefixType.Command, new string[] { "no bully", "stop bully" }, "Tell me to enforce a no bully zone", Conversation.NoBully),
                new Command(Command.PrefixType.Command, "sing", "Ask me to sing", Conversation.Sing),
                new Command(Command.PrefixType.Command, "dance", "Ask me to dance", Conversation.Dance),
                new Command(Command.PrefixType.Command, new string[] { "good night", "bye" }, "Say good night to me", Conversation.GoodNight),
                new Command(Command.PrefixType.Command, new string[] { "watch out", "trip", "stop" }, "Will make me trip", Conversation.Trip),
                new Command(Command.PrefixType.Command, new string[] { "you're weird", "you are weird" }, "Make me sad", Conversation.Weird),
                new Command(Command.PrefixType.Command, new string[] { "you're cute", "you are cute" }, "Make me happy", Conversation.Cute),
                new Command(Command.PrefixType.Command, new string[] { "do you even lewd", "try to be lewd" }, "Send semi-lewd pictures", Conversation.Lewd),
                new Command(Command.PrefixType.Command, new string[] { "what's", "what is", "who's", "who is" }, "Search for a term", Search.Define),
                new Command(Command.PrefixType.Command, new string[] { "what are", "what're" }, "Search for a plural term", Search.DefineSimple),
                new Command(Command.PrefixType.Mention, "shitpost", "Send a shitpost", Conversation.Shitpost),
                new Command(Command.PrefixType.Mention, new string[] { "send oc", "stealie" }, "Stealie a mealie", Conversation.Dogman),
                new Command(Command.PrefixType.Command, "meme", "Memeify a text", Conversation.Meme),*/

                new Command(Command.PrefixType.None, "megane", "", "Fuyukai desu!"),
                new Command(Command.PrefixType.None, "burn the chat", "", "🔥 ด้้้้้็็็็็้้้้้็็็็็้้้้้้้้็็็็็้้้้้็็ด้้้้้็็็็็้้้้้็็็็็้้้้้้้้็็็็็้้้้้็็็็็้้้้้้้้็็็ด้้้้้็็็็็้้้้้็็็็็้้้้้้้้็็็็็้้้้้็็็็็้้้้ 🔥"),
                new Command(Command.PrefixType.None, "kuriyama?", "", "Yes?"),
                new Command(Command.PrefixType.None, "mirai?", "", "Please call me Kuriyama"),
                //new Command(Command.PrefixType.None, new string[] { "fuck mirai", "fuck you mirai" }, "", Conversation.Cry),

                new Command(Command.PrefixType.None, new [] { "aaaae", "o-ooo" }, "", "**__" + FullSpam + "__**"),
                new Command(Command.PrefixType.None, "fap", "", "ಠ.ಠ"),
                new Command(Command.PrefixType.None, "fuyukai desu", "", "That's my joke!"),
                new Command(Command.PrefixType.None, "\\o\\", "", "/o/"),
                new Command(Command.PrefixType.None, "/o/", "", "\\o\\"),
                new Command(Command.PrefixType.None, "\\o/", "", "\\o/"),
                new Command(Command.PrefixType.None, "/lenny", "", "( ͡° ͜ʖ ͡°)"),
                //new Command(Command.PrefixType.None, "$$$", "", Trivia.Points)
            });
        }

        /*internal static async Task ToggleCategory(FeedContext Feed, ReceivedMessage Message)
        {
            var Category = Message.Text.Trim();

            if (Category != string.Empty && Categories.ContainsKey(Category))
            {
                //Toggle Category
            }
        }*/

        internal static async Task Help(ReceivedMessage Message)
        {
            foreach (var Category in Categories)
            {
                var CategoryCommands = string.Empty;

                foreach (var Command in Category.Value)
                {
                    if (Command.Prefix == Command.PrefixType.None)
                    {
                        continue;
                    }
                    
                    CategoryCommands += string.Join(" or ", Command.Keys) + " ~ `" + Command.Description + "`\n";
                }

                if (CategoryCommands != string.Empty)
                {
                    Message.Respond((Category.Key == string.Empty ? "**Main**" : "**" + Category.Key + "**") + "\n" + CategoryCommands);
                    await Task.Delay(200);
                }
            }
        }
    }
}
