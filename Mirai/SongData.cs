using Google.Apis.YouTube.v3;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using VideoLibrary;

namespace Mirai
{
    class SongData
    {
        internal static YouTubeService YT;
        internal static string MusicDir = Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%") + "\\Music\\";
        internal static string[] LocalFiles(string Search)
        {
            return new DirectoryInfo(MusicDir).GetFiles()
                .Where(x => x.Name.Length >= Search.Length && x.Name.ToLower().Contains(Search.ToLower()) && !x.Attributes.HasFlag(FileAttributes.System))
                .Select(x => x.Name)
                .OrderBy(x => x)
                .ToArray();
        }

        internal bool Found;
        internal string Query;
        internal string Title
        {
            get
            {
                if (FullName.Length < 60)
                {
                    return FullName;
                }

                return FullName.Substring(0, 60);
            }
        }
        internal string FullName;
        internal string Url;

        internal SongData(object ToSearch)
        {
            Found = false;
            Query = ((string)ToSearch).Trim();
            FullName = Query;
            Url = Query;

            if (Query == string.Empty)
            {
                return;
            }

            try
            {
                var Local = LocalFiles(Query);
                if (Local.Length > 0 && Local.Contains(Query))
                {
                    Url = MusicDir + Query;
                    Found = true;
                    return;
                }

                if (Regex.IsMatch(Query, "(.*)(soundcloud.com|snd.sc)(.*)"))
                {
                    var SC = ("http://api.soundcloud.com/resolve?url=" + Query + "&client_id=" + Bot.Config["SoundCloud"]).WebResponse();
                    if (SC != string.Empty && SC.StartsWith("{\"kind\":\"track\""))
                    {
                        var Response = JObject.Parse(SC);
                        FullName = Response["title"].ToString();
                        Url = Response["stream_url"] + "?client_id=" + Bot.Config["SoundCloud"];
                        Found = true;
                    }

                    return;
                }

                string YouTubeUrl = string.Empty;
                if (Query.IsValidUrl())
                {
                    if (Regex.IsMatch(Query, @"http(s)?://(www\.)?(youtu\.be|youtube\.com)[\w-/=&?]+"))
                    {
                        YouTubeUrl = Query;
                    }
                    else
                    {
                        Found = true;
                        return;
                    }
                }
                else
                {
                    var ListRequest = YT.Search.List("snippet");
                    ListRequest.Q = Query;
                    ListRequest.MaxResults = 1;
                    ListRequest.Type = "video";
                    foreach (var Result in ListRequest.Execute().Items)
                    {
                        YouTubeUrl = "http://www.youtube.com/watch?v=" + Result.Id.VideoId;
                    }
                }

                if (YouTubeUrl != string.Empty)
                {
                    var Videos = YouTube.Default.GetAllVideos(YouTubeUrl);

                    var Adaptive = Videos.Where(Video => Video.AdaptiveKind == AdaptiveKind.Audio);
                    if (Adaptive.Count() > 0)
                    {
                        Videos = Adaptive;
                    }

                    Videos = Videos.OrderByDescending(v => v.AudioBitrate);

                    if (Videos.Count() > 0)
                    {
                        var Video = Videos.First();
                        FullName = Video.Title.Substring(0, Video.Title.Length - 10);
                        Url = Video.Uri;
                        Found = true;
                    }
                }
            }
            catch { }
        }
    }
}
