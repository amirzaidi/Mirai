using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Mirai.Commands
{
    class Lewd
    {
        public static async Task Search(ReceivedMessage Message)
        {
            var RNG = new Random();

            string Query = Message.Text;
            if (Query.StartsWith("."))
            {
                Query = Query.Substring(1);
            }
            else
            {
                Query += "+sex";
            }

            if (Query.Contains("loli"))
            {
                Query = Query.Replace("loli", "flat chest");
            }

            Query = Query.Replace(" ", "_");

            var Result = ("http://danbooru.donmai.us/posts/random?tags=" + Query).WebResponse();
            var Matches = Regex.Matches(Result, "data-large-file-url=\"(?<id>.*?)\"");
            if (Matches.Count > 0 && (!Result.ToLower().Contains("kyoukai") && !Result.ToLower().Contains("kuriyama")))
            {
                await Message.Respond("http://danbooru.donmai.us" + Matches[RNG.Next(0, Matches.Count)].Groups["id"].Value, false);
            }
            else
            {
                Result = ("http://gelbooru.com/index.php?page=post&s=list&pid=0&tags=" + Query).WebResponse();
                Matches = Regex.Matches(Result, "span id=\"s(?<id>\\d*)\"");
                if (Matches.Count > 0 && (!Result.ToLower().Contains("kyoukai") && !Result.ToLower().Contains("kuriyama")))
                {
                    await Message.Respond(Regex.Match(("http://gelbooru.com/index.php?page=post&s=view&id=" + Matches[RNG.Next(0, Matches.Count)].Groups["id"].Value).WebResponse(), "\"(?<url>http://simg4.gelbooru.com//images.*?)\"").Groups["url"].Value, false);
                }
                else
                {
                    await Message.Respond("I couldn't find anything");
                }
            }
        }
    }
}
