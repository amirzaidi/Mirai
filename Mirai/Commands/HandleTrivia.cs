using Mirai.Handlers;
using System.Linq;
using System.Threading.Tasks;

namespace Mirai.Commands
{
    class Trivia
    {
        internal static async Task Start(ReceivedMessage Message)
        {
            if (Message.Feed.Trivia.Running == 0)
            {
                Message.Feed.Trivia.Points.Clear();
                Message.Feed.Trivia.Running = 1;
                await Message.Feed.SendAll($"Welcome to the trivia! To win, you need {TriviaHandler.PointLimit} points");
            }
        }

        internal static async Task Answer(ReceivedMessage Message)
        {
            if (Message.Feed.Trivia.Running != 0 && Message.Feed.Trivia.Winner == null && Message.Text.ToLower().TrimStart('.').Contains(Message.Feed.Trivia.Answer.ToLower()))
            {
                Message.Feed.Trivia.Winner = Message.SenderMention;
                await Message.Feed.SendAll($"{Message.Feed.Trivia.Answer} is correct {Message.SenderMention} - added one pair of glasses");
            }
        }

        internal static async Task Points(ReceivedMessage Message)
        {
            if (Message.Feed.Trivia.Running != 0)
            {
                string Text;
                if (Message.Feed.Trivia.Points.ContainsKey(Message.SenderMention))
                {
                    Text = $"You have {Message.Feed.Trivia.Points[Message.SenderMention]} points";
                }
                else
                {
                    Text = $"You have 0 points";
                }

                foreach (var KVP in Message.Feed.Trivia.Points.OrderBy(u => -u.Value))
                {
                    if (KVP.Key != Message.SenderMention)
                    {
                        Text += $"\n{KVP.Key} has {KVP.Value} point(s)";
                    }
                }

                await Message.Respond(Text);
            }
        }

        internal static async Task Stop(ReceivedMessage Message)
        {
            if (Message.Feed.Trivia.Running != 0 && !Message.Feed.Trivia.AbortRequested)
            {
                Message.Feed.Trivia.AbortRequested = true;
                await Message.Feed.SendAll("Trivia will stop after this round");
            }
        }
    }
}
