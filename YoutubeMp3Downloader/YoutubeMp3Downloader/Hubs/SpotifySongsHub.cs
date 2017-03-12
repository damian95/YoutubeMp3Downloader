using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using YoutubeMp3Downloader.Models;
using HtmlAgilityPack;

namespace YoutubeMp3Downloader.Hubs
{
    public class SpotifySongsHub : Hub
    {
        //get the songs associated with the playlist selected
        public async Task<int> getPlaylistSongs(string playlistID, string token, long userID)
        {
            try
            {
                //send hhtp request to spotify to get the songs of playlist that belongs to user
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    var request = await client.GetAsync("https://api.spotify.com/v1/users/" + userID + "/playlists/" + playlistID + "/tracks");
                    Clients.Caller.getPlaylistSongs(getSongs(request));
                }
                return 1;
            }
            catch (Exception e)
            {
                //send hhtp request to spotify to get the songs of public playlist
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    var request = await client.GetAsync("https://api.spotify.com/v1/users/" + "spotify/playlists/" + playlistID + "/tracks");
                    Clients.Caller.getPlaylistSongs(getSongs(request));
                }
                return 1;
            }

        }

        //get the youtueb results of the songs requested
        public async Task<int> getSearchedSongs(string songName, string artistname)
        {
            string title = songName + " " + artistname;
            string[] tokens = title.Split(' ');
            title = "https://www.youtube.com/results?search_query=";
            foreach (var token in tokens)
            {
                title = title + token + "+";
            }
            List<YoutubeVid> vids = new List<YoutubeVid>();
            vids = await startCrawler(title);
            Clients.Caller.getSearchedSongs(vids);
            return 1;
        }

        //get list of the songs from http response from spotify server for a playlist
        public List<SpotifyTrack> getSongs(HttpResponseMessage request)
        {
            List<SpotifyTrack> songs = new List<SpotifyTrack>();
            JObject jsongRequest = (JObject)JsonConvert.DeserializeObject(request.Content.ReadAsStringAsync().Result);
            foreach (var song in jsongRequest["items"])
            {
                SpotifyTrack track = new SpotifyTrack()
                {
                    name = song["track"]["name"].ToString(),
                    artist = song["track"]["artists"][0]["name"].ToString()
                };
                songs.Add(track);
            }
            
            return songs;
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
                //ViewBag.Message = e.Message;
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
                        }
                        else if (img.Descendants("img").FirstOrDefault().Attributes["data-thumb"].Value.StartsWith("https"))
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