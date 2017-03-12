using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using YoutubeExtractor;
using YoutubeMp3Downloader.Models;
using Frapper;
using System.Text.RegularExpressions;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace YoutubeMp3Downloader.Controllers
{
    public class HomeController : Controller
    {
        //Iinital page load
        [HttpGet]
        public ActionResult Index()
        {
            //create a list for Index page to recieve that will be empty when page on a clean page
            List<YoutubeVid> model = new List<YoutubeVid>();
            YoutubeVid vid = new YoutubeVid()
            {
                url = "",
                imgLink = ""
            };
            model.Add(vid);
            return View(model);
        }
        //fetch videos realated to key words searched for
        [HttpPost]
        public async Task<ActionResult> Index(List<YoutubeVid> model)
        {
            try
            {
                //get each indivual key word searched for
                string[] tokens = model[0].url.Split(' ');
                //set the base url
                model[0].url = "https://www.youtube.com/results?search_query=";

                //create the new url from the base url and searched words
                foreach (var token in tokens)
                {
                    model[0].url = model[0].url + token + "+";
                }

                //store the youtube videos and their information into list that will be returned to client
                List<YoutubeVid> vids = new List<YoutubeVid>();

                vids = await startCrawler(model[0].url);

                return View(vids);
            }catch(Exception e)
            {
                return View(model);
            }
        }

        //Controller for the view that will handle fetching the mp3 from the server
        public ActionResult DownloadError(Exception e)
        {
            return View(e);
        }

        //download the mp3 file from the server
        public ActionResult Downlaod(string id)
        {
            try
            {
                //conver the song title to charArray to add the period back at the end of the file name
                var songTitle = id.ToCharArray();
                songTitle[songTitle.Length - 4] = '.';

                //create the path where the file is stored and send the file to client
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory + "Downloads", new string(songTitle));
                var bytes = new byte[0];
                FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                BinaryReader br = new BinaryReader(fs);
                long numBytes = new FileInfo(path).Length;
                var buff = br.ReadBytes((int)numBytes);
                fs.Close();
                System.IO.File.Delete(path);

                string[] pathArray = path.Split('\\');

                return File(buff, "audio/mpeg", pathArray.Last());
            }catch(Exception e)
            {
                return RedirectToAction("DownloadError", e);
            }            
        }

        public async Task<ActionResult> Spotify()
        {
            //get code value from url
            var code = Request.Url.Query.Remove(0, 6);

            using (var client = new HttpClient())
            {
                //body of HttpPost to get roken for user
                var values = new Dictionary<string, string>
                {
                    {"grant_type", "authorization_code"},
                    {"code", code},
                    {"redirect_uri", "http://localhost:2177/Home/Spotify"},
                    {"client_id", "bf9be64550ab43a3a45cbdc762780c05"},
                    {"client_secret", "3999dba5632c4ac7af025b7cbb574805"}
                };

                //get the token of the user trying to sign in
                var content = new FormUrlEncodedContent(values); 
                var response = await client.PostAsync("https://accounts.spotify.com/api/token", content);
                var responseToken = await response.Content.ReadAsStringAsync();
                SpotifyToken token = JsonConvert.DeserializeObject<SpotifyToken>(responseToken);

                //get users playlists
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.access_token);
                var responsePlaylist = await client.GetAsync("https://api.spotify.com/v1/me/playlists");
                JObject jsonPlaylist = (JObject)JsonConvert.DeserializeObject(responsePlaylist.Content.ReadAsStringAsync().Result);

                //get users spotify id
                var responseUser = await client.GetAsync("https://api.spotify.com/v1/me");
                JObject jsonUser = (JObject)JsonConvert.DeserializeObject(responseUser.Content.ReadAsStringAsync().Result);

                List<SpotifyPlayList> playlists = new List<SpotifyPlayList>();

                for (int i = 0; i < jsonPlaylist["items"].Count(); i++)
                {
                    SpotifyPlayList sp = new SpotifyPlayList()
                    {
                        playListId = jsonPlaylist["items"][i]["id"].ToString(),
                        userId = Convert.ToInt64(jsonUser["id"]),
                        name = jsonPlaylist["items"][i]["name"].ToString(),
                        token = token.access_token
                    };

                    playlists.Add(sp);
                }
                //return View((object)sp["items"][0]["name"].ToString());

                return View(playlists);

            }

        }

        //web crawl and grab search result page info
        private async Task<List<YoutubeVid>> startCrawler(string url)
        {
            //prepare the object and instantiate the two lists
            List<YoutubeVid> model = new List<YoutubeVid>();

            //prepare the web crawler
            var httpClient = new HttpClient();
            var html = await httpClient.GetStringAsync(url);
            var htmlDocument = new HtmlDocument();

            try
            {
                htmlDocument.LoadHtml(html);
            }
            catch (Exception e)
            {
                ViewBag.Message = e.Message;
            }

            //divs contains all the video information
            var divs = htmlDocument.DocumentNode.Descendants("div")
                .Where(node => node.GetAttributeValue("class", "")
                .Equals("yt-lockup-content")).ToList();

            //imgs contains the img of the video
            var imgs = htmlDocument.DocumentNode.Descendants("span")
                .Where(node => node.GetAttributeValue("class", "")
                .Equals("yt-thumb-simple")).ToList();

            //itterate through each div and store into an object taht will eba dded to a list
            foreach (var div in divs)
            {
                try
                {
                    var titleUrl = div.Descendants("h3")
                        .Where(node => node.GetAttributeValue("class", "")
                        .Equals("yt-lockup-title ")).FirstOrDefault();

                    var badlink = titleUrl.Descendants("a").FirstOrDefault().Attributes["class"].Value;

                    if (badlink.Equals("yt-uix-tile-link yt-ui-ellipsis yt-ui-ellipsis-2 yt-uix-servicelink") || !titleUrl.Descendants("span").FirstOrDefault().InnerText.Equals(" - Playlist"))
                    {

                        var uploader = div.Descendants("div")
                            .Where(node => node.GetAttributeValue("class", "")
                            .Equals("yt-lockup-byline ")).FirstOrDefault();

                        var description = div.Descendants("div")
                            .Where(node => node.GetAttributeValue("class", "")
                            .Equals("yt-lockup-description yt-ui-ellipsis yt-ui-ellipsis-2")).FirstOrDefault();

                        string _title = titleUrl.Descendants("a").FirstOrDefault().Attributes["title"].Value;

                        _title = cleanseTitle(_title);

                        var img = imgs[divs.IndexOf(div)];

                        string _imgLink = "";

                        if (img.Descendants("img").FirstOrDefault().Attributes["src"].Value.StartsWith("https"))
                        {
                            _imgLink = img.Descendants("img").FirstOrDefault().Attributes["src"].Value;
                        }else if (img.Descendants("img").FirstOrDefault().Attributes["data-thumb"].Value.StartsWith("https"))
                        {
                            _imgLink = img.Descendants("img").FirstOrDefault().Attributes["data-thumb"].Value;
                        }

                        YoutubeVid vid = new YoutubeVid()
                        {
                            id = divs.IndexOf(div),
                            title = _title,
                            duration = titleUrl.Descendants("span").FirstOrDefault().InnerText,
                            description = description.InnerText,
                            uploader = uploader.Descendants("a").FirstOrDefault().InnerText,
                            url = titleUrl.Descendants("a").FirstOrDefault().Attributes["href"].Value,
                            imgLink = _imgLink
                        };

                        model.Add(vid);
                    }
                }
                catch (Exception e)
                {

                }
                
            }
            return model;
        }

        //cleare some of the unwanted characted added onto the song title
        private string cleanseTitle(string title)
        {
            int index = title.IndexOf("amp;");
            while (index != -1)
            {
                title = title.Remove(index, 4);
                index = title.IndexOf("amp;");
            }
            index = title.IndexOf("&quot;");
            while (index != -1)
            {
                title = title.Remove(index, 6);
                index = title.IndexOf("&quot;");
            }
            index = title.IndexOf("&#39;");
            while (index != -1)
            {
                title = title.Remove(index, 5);
                index = title.IndexOf("&#39;");
            }
            return title;
        }
    }

}