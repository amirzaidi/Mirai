using Discord.Net.Rest;
using Imgur.API.Authentication.Impl;
using Imgur.API.Endpoints.Impl;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Mirai.Commands
{
    class Search
    {
        internal static async Task Osu(ReceivedMessage Message)
        {
            var client = new ImgurClient(Bot.Config["ImgurId"], Bot.Config["ImgurSecret"]);
            var endpoint = new ImageEndpoint(client);
            var Image = await endpoint.UploadImageUrlAsync($"http://lemmmy.pw/osusig/sig.php?uname={Message.Text}&flagshadow&xpbar&xpbarhex&pp=2");
            await Message.Respond($"{Image.Link}\nhttps://osu.ppy.sh/u/{Uri.EscapeDataString(Message.Text)}", false);
        }

        internal static async Task Define(ReceivedMessage Message)
        {
            var Query = Message.Text;
            if (Query.StartsWith("a "))
            {
                Query = Query.Substring(2);
            }
            else if (Query.StartsWith("an "))
            {
                Query = Query.Substring(3);
            }

            if (Query != string.Empty)
            {
                try
                {
                    var Headers = new WebHeaderCollection();
                    Headers.Add("X-Mashape-Key", Bot.Config["Mashape"]);
                    var Json = JObject.Parse(await $"https://mashape-community-urban-dictionary.p.mashape.com/define?term={Uri.EscapeUriString(Query)}".WebResponse(Headers));
                    Message.Respond(Json["list"][0]["definition"].ToString());
                }
                catch
                {
                    Message.Respond("I couldn't find it");
                }
            }
        }

        internal static async Task Anime(ReceivedMessage Message)
        {
            string AniToken;
            var API = GetAniApi(out AniToken);
            var s = Message.Text.Replace('/', ' ').Trim();

            var SearchRequest = new RestRequest("/anime/search/" + Uri.EscapeUriString(s));
            SearchRequest.AddParameter("access_token", AniToken);
            var SearchResString = API.Execute(SearchRequest).Content;

            if (SearchResString.Trim() != string.Empty && JToken.Parse(SearchResString) is JArray)
            {
                RestRequest InfoRequest = new RestRequest("/anime/" + JArray.Parse(SearchResString)[0]["id"]);
                InfoRequest.AddParameter("access_token", AniToken);

                JObject Info = JObject.Parse(API.Execute(InfoRequest).Content);

                string Title = "`" + Info["title_romaji"] + "`";
                if (Title != Info["title_english"].ToString())
                {
                    Title += " / `" + Info["title_english"] + "`";
                }

                string Extra = "";
                if (Info["total_episodes"].ToString() != "0" && Info["average_score"].ToString() != "0")
                {
                    Extra = Info["total_episodes"] + " Episodes (" + Info["airing_status"] + ") - Scored " + Info["average_score"] + "\n";
                }

                Message.Respond(Title + "\n" + Extra +
                    "Synopsis: " + WebUtility.HtmlDecode(Info["description"].ToString()).Replace("<br>", "\n").MaxSubstring(500, "...") + "\n" +
                    "More info at http://anilist.co/anime/" + Info["id"] + "\n" + Info["image_url_lge"]);
            }
            else
            {
                Message.Respond("The anime could not be found");
            }
        }

        internal static async Task Manga(ReceivedMessage Message)
        {
            string AniToken;
            var API = GetAniApi(out AniToken);
            var s = Message.Text.Replace('/', ' ').Trim();

            if (!s.StartsWith("@"))
            {
                var SearchRequest = new RestRequest("/manga/search/" + Uri.EscapeUriString(s));
                SearchRequest.AddParameter("access_token", AniToken);
                var SearchResString = API.Execute(SearchRequest).Content;

                if (SearchResString.Trim() != string.Empty && JToken.Parse(SearchResString) is JArray)
                {
                    RestRequest InfoRequest = new RestRequest("/manga/" + JArray.Parse(SearchResString)[0]["id"]);
                    InfoRequest.AddParameter("access_token", AniToken);

                    JObject Info = JObject.Parse(API.Execute(InfoRequest).Content);

                    string Title = "`" + Info["title_romaji"] + "`";
                    if (Title != Info["title_english"].ToString())
                    {
                        Title += " / `" + Info["title_english"] + "`";
                    }

                    string Extra = "";
                    if (Info["total_chapters"].ToString() != "0" && Info["average_score"].ToString() != "0")
                    {
                        Extra = Info["total_chapters"] + " Chapters (" + Info["publishing_status"] + ") - Scored " + Info["average_score"] + "\n";
                    }

                    Message.Respond(Title + "\n" + Extra +
                        "Synopsis: " + WebUtility.HtmlDecode(Info["description"].ToString()).Replace("<br>", "\n").MaxSubstring(500, "...") + "\n" +
                        "More info at http://anilist.co/manga/" + Info["id"] + "\n" + Info["image_url_lge"]);
                }
                else
                {
                    Message.Respond("The manga could not be found");
                }
            }
        }

        internal static async Task AnimeShort(ReceivedMessage Message)
        {
            var PosStart = Message.Text.IndexOf('{');
            var PosEnd = Message.Text.IndexOf('}');
            if (PosEnd - PosStart > 1)
            {
                Message.Text = Message.Text.Substring(++PosStart, PosEnd - PosStart);
                Anime(Message);
            }
        }

        internal static async Task MangaShort(ReceivedMessage Message)
        {
            var PosStart = Message.Text.IndexOf('<');
            var PosEnd = Message.Text.IndexOf('>');
            if (PosEnd - PosStart > 1)
            {
                Message.Text = Message.Text.Substring(++PosStart, PosEnd - PosStart);
                Manga(Message);
            }
        }

        private static RestSharp.RestClient GetAniApi(out string AniToken)
        {
            var API = new RestSharp.RestClient("http://anilist.co/api");

            var TokenRequest = new RestRequest("/auth/access_token", Method.POST);
            TokenRequest.AddParameter("grant_type", "client_credentials");
            TokenRequest.AddParameter("client_id", Bot.Config["AniId"]);
            TokenRequest.AddParameter("client_secret", Bot.Config["AniSecret"]);
            AniToken = JObject.Parse(API.Execute(TokenRequest).Content)["access_token"].ToString();

            return API;
        }
    }
}
