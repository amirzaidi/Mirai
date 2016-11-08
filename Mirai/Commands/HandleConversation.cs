using Imgur.API.Authentication.Impl;
using Imgur.API.Endpoints.Impl;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Mirai.Commands
{
    class Conversation
    {
        private static Random Rand = new Random();

        internal async static Task Choose(ReceivedMessage Message)
        {
            if (Message.Text.StartsWith("between"))
            {
                Message.Text = Message.Text.Substring("between").TrimStart();
            }

            if (Message.Text != string.Empty)
            {
                var Split = Message.Text.Replace(" and ", ",").Split(',');
                Message.Respond("I choose " + Split[new Random().Next(0, Split.Length)].Trim());
            }
        }

        private static string[] Insults = new []
        {
            " is a faggot",
            " is a weeaboo",
            " has shit taste",
            "'s waifu is trash",
            " your memes aren't even dank",
            " you weird glasses-fetishist",
            " your ass is jealous of the amount of shit that just came out of your mouth",
            " you'll never be the man your mother is",
            " no one will look for you during hide and seek",
            " I'd stab you but shit stains"
        };

        internal async static Task Insult(ReceivedMessage Message)
        {
            if (Message.Mentions.Length == 1)
            {
                if (Message.Mentions[0].Id == Bot.Clients[Message.Origin.Token].Owner)
                {
                    await Message.Respond("I would never do that!");
                }
                else
                {
                    await Message.Respond(Message.Mentions[0].Mention + Insults[Rand.Next(0, Insults.Length)]);
                }
            }
        }

        private static string[] StabImgs = new []
        {
            "http://vignette1.wikia.nocookie.net/kyoukainokanata/images/5/5e/Stabbing-Akihito.png/revision/latest?cb=20131110041214",
            "https://s-media-cache-ak0.pinimg.com/736x/1b/4a/2e/1b4a2e27fe20c0152131504a73498dd1.jpg"
        };

        internal async static Task Stab(ReceivedMessage Message)
        {
            if (Message.Mentions.Length == 1)
            {
                if (Message.Mentions[0].Id == Bot.Clients[Message.Origin.Token].Owner)
                {
                    await Message.Respond("I would never do that!");
                }
                else
                {
                    await Message.Respond(Message.Mentions[0].Mention + " " + StabImgs[Rand.Next(0, StabImgs.Length)], false);
                }
            }
        }
        
        private static string[] RewardImgs = new []
        {
            "http://24.media.tumblr.com/127501c52a5eed326b363ed85087f777/tumblr_mus8rkat4J1qkcwzfo1_250.gif",
            "https://s-media-cache-ak0.pinimg.com/originals/e6/d5/8d/e6d58d221ae3df61580d95b34558e2d8.gif"
        };

        internal async static Task Reward(ReceivedMessage Message)
        {
            Message.Respond(RewardImgs[Rand.Next(0, RewardImgs.Length)], false);
        }

        private static string[] SingImgs = new[]
        {
            "http://25.media.tumblr.com/50a3b7c75244eb0882e5706474489d7a/tumblr_mvzx00vMG61r2b9f8o1_400.gif",
            "http://25.media.tumblr.com/47e605a8b088ca71a1713838a11e736a/tumblr_mvw7o93rfK1soj51lo6_250.gif"
        };

        internal async static Task Sing(ReceivedMessage Message)
        {
            Message.Respond(SingImgs[Rand.Next(0, SingImgs.Length)], false);
        }

        private static string[] DanceImgs = new []
        {
            "http://2.bp.blogspot.com/-zZw7eAzkOEM/U6HVaOztqZI/AAAAAAAAaD0/zZny7-LTDsE/s1600/tumblr_inline_mvv1cu8lDl1rrn5dn.gif",
            "https://media.giphy.com/media/MDEWuO3nwkG4w/giphy.gif",
            "http://25.media.tumblr.com/07a02819a2bcc8b85c04df36ba324c4b/tumblr_mvv2oaj8To1rxaojso5_250.gif"
        };

        internal async static Task Dance(ReceivedMessage Message)
        {
            Message.Respond(DanceImgs[Rand.Next(0, DanceImgs.Length)], false);
        }

        internal async static Task Meme(ReceivedMessage Message)
        {
            var client = new ImgurClient(Bot.Config["ImgurId"], Bot.Config["ImgurSecret"]);
            var endpoint = new AlbumEndpoint(client);
            var images = await endpoint.GetAlbumImagesAsync("AEWxy");
            var imagesArray = images.ToArray();
            
            Message.Respond(imagesArray[Rand.Next(0, imagesArray.Length)].Link, false);
        }
    }
}
