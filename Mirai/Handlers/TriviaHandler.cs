using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System;

namespace Mirai.Handlers
{
    class TriviaHandler : IHandler
    {
        private FeedContext Feed;
        internal Dictionary<string, int> Points = new Dictionary<string, int>();
        private static Regex RemoveHtml = new Regex("<.*?>", RegexOptions.Compiled);

        internal string Answer = null;
        internal const int PointLimit = 10;

        internal int Running = 0;
        internal bool AbortRequested = false;
        private Task<string> GetQuestion = null;

        private Stopwatch Timer = null;
        internal string Winner = null;

        public string EasyHint
        {
            get
            {
                if (Answer == null)
                {
                    return string.Empty;
                }

                var Letters = Answer.ToArray();
                var Count = 0;
                for (var i = 1; i < Letters.Length; i++)
                {
                    if (char.IsLetterOrDigit(Letters[i]) && ++Count % 3 == 2)
                    {
                        Letters[i] = '_';
                    }

                }

                return string.Join(" ", Letters);
            }
        }

        public string HardHint
        {
            get
            {
                if (Answer == null)
                {
                    return string.Empty;
                }

                var Letters = Answer.ToArray();
                var Count = 0;
                for (int i = 1; i < Letters.Length; i++)
                {
                    if (char.IsLetterOrDigit(Letters[i]) && ++Count % 3 != 0)
                    {
                        Letters[i] = '_';
                    }

                }

                return string.Join(" ", Letters);
            }
        }

        public TriviaHandler(FeedContext Feed)
        {
            this.Feed = Feed;
        }

        public async Task Tick()
        {
            if (Running != 0)
            {
                if (Points.ContainsValue(PointLimit))
                {
                    await GetWinner();
                }
                else if (GetQuestion != null)
                {
                    if (GetQuestion.IsCompleted)
                    {
                        var Result = GetQuestion.Result;
                        GetQuestion = null;

                        var Trivia = JObject.Parse(Result.Substring(1, Result.Length - 2));
                        var Question = Trivia["question"].ToString().Trim();

                        if (Question != string.Empty)
                        {
                            Answer = RemoveHtml.Replace(Trivia["answer"].ToString(), string.Empty).Replace("\\", "").Replace("(", "").Replace(")", "").Trim('"');
                            if (Answer.StartsWith("a "))
                            {
                                Answer = Answer.Substring("a ");
                            }
                            else if (Answer.StartsWith("an "))
                            {
                                Answer = Answer.Substring("an ");
                            }

                            await Feed.SendAll($"Trivia | {Question}");
                            Timer = new Stopwatch();
                            Timer.Start();

                            Bot.Log($"{Question} - {Answer}");
                        }
                    }
                }
                else if (Timer == null)
                {
                    if (AbortRequested)
                    {
                        AbortRequested = false;
                        await GetWinner();
                    }
                    else
                    {
                        GetQuestion = "http://jservice.io/api/random?count=1".WebResponse();
                        Running = 1;
                    }
                }
                else if (Winner != null)
                {
                    if (!Points.ContainsKey(Winner))
                    {
                        Points.Add(Winner, 0);
                    }

                    Points[Winner]++;
                    Winner = null;
                    Timer = null;
                }
                else if (Timer.ElapsedMilliseconds > 10000)
                {
                    if (Running == 1)
                    {
                        await Feed.SendAll($"Hint: `{HardHint}`");
                        Timer.Restart();
                        Running++;
                    }
                    else if (Running == 2)
                    {
                        await Feed.SendAll($"Hint: `{EasyHint}`");
                        Timer.Restart();
                        Running++;
                    }
                    else if (Running == 3)
                    {
                        await Feed.SendAll($"Time's up! The answer was `{Answer}`");
                        Timer = null;
                    }
                }
            }
        }

        private async Task GetWinner()
        {
            Running = 0;

            var Send = string.Empty;
            var Winner = Points.OrderBy(u => u.Value).LastOrDefault();

            if (Winner.Value != 0)
            {
                Feed.SendAll($"{Winner.Key} won the trivia with {Winner.Value} glasses!");
            }
        }

        public async Task Save()
        {
        }
    }
}
