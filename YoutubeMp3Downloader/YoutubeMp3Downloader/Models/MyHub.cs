using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using YoutubeMp3Downloader.Controllers;
using YoutubeExtractor;
using System.IO;
using System.Text.RegularExpressions;
using Frapper;

namespace YoutubeMp3Downloader.Models
{
    public class MyHub : Hub
    {

        public void announce(string link)
        {
            try
            {
                //crea the url of the video requested to be downloaded
                string url = "https://www.youtube.com/watch?v=" + link;
                IEnumerable<VideoInfo> videoInfos = DownloadUrlResolver.GetDownloadUrls(url);

                VideoInfo video = videoInfos.First(info => info.VideoType == VideoType.Mp4 && info.Resolution == 0);
                if (video.RequiresDecryption)
                {
                    DownloadUrlResolver.DecryptDownloadUrl(video);
                }

                //download the video and get the path of the file
                string path = tryVidDownloadExe(video);

                //if the video downloaded successfully convert it to mp3
                if (path != null)
                {
                    path = Mp4ToMp3(path, 15);
                }

                //get the mp3 file name from the path and remove the persion from the file name
                string[] pathArray = path.Split('\\');
                string vidTitle = pathArray.Last();

                vidTitle = RemoveAllChar('.', vidTitle);

                Clients.Caller.announce(vidTitle);
                }
            catch (Exception e)
            {
                Clients.Caller.announce(link, e.Message, e.HResult, e.Source);
            }
        }

        private static string tryVidDownloadExe(VideoInfo video)
        {

            try
            {
                //remove all illegal character from video name that will throw an error when returned to the server
                var vidTitle = video.Title;
                vidTitle = RemoveAllChar('.', vidTitle);
                vidTitle = RemoveAllChar('\\', vidTitle);
                vidTitle = RemoveAllChar('/', vidTitle);
                vidTitle = RemoveAllChar('#', vidTitle);
                vidTitle = RemoveAllChar('|', vidTitle);
                vidTitle = RemoveAllChar('&', vidTitle);
                vidTitle = RemoveAllChar('%', vidTitle);
                vidTitle = RemoveAllChar('*', vidTitle);
                vidTitle = RemoveAllChar(':', vidTitle);
                vidTitle = RemoveAllChar('?', vidTitle);
                vidTitle = RemoveAllChar('<', vidTitle);
                vidTitle = RemoveAllChar('>', vidTitle);
                vidTitle = RemoveAllChar('+', vidTitle);

                //create the path for the video to be stored and attempt to download it, if successful return the path of the video
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory + "Downloads", vidTitle + video.VideoExtension);
                for (int i = 1; System.IO.File.Exists(path); i++)
                {
                    path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory + "Downloads", video.Title + "(" + i + ")" + video.VideoExtension);
                }
                RemoveIllegalPathCharacters(path);
                var videoDownloader = new VideoDownloader(video, path);
                videoDownloader.Execute();
                return path;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        //remove illegal characters from path
        private static string RemoveIllegalPathCharacters(string path)
        {
            string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            var r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
            return r.Replace(path, "");
        }


        //convert the mp4 video to mp3
        private static string Mp4ToMp3(string path, int seconds = 10)
        {
            string result;

            return Mp4ToMp3(path, seconds, out result);
        }

        //convert the mp4 video to mp3
        private static string Mp4ToMp3(string path, int seconds, out string result)
        {
            FFMPEG ffmpeg = new FFMPEG();

            // Convert to wav.
            string wavPath = path.Replace(".mp4", ".mp3");
            result = ffmpeg.RunCommand("-i \"" + path + "\" \"" + wavPath + "\"");

            // Cleanup.
            System.IO.File.Delete(path);

            return wavPath;
        }

        //remove all chars from string
        static string RemoveAllChar(Char target, String searched)
        {
            char[] vidTitle = searched.ToCharArray();
            int startIndex = -1;

            // Search for all occurrences of the target.
            while (true)
            {
                startIndex = searched.IndexOf(
                    target, startIndex + 1,
                    searched.Length - startIndex - 1);

                // Exit the loop if the target is not found.
                if (startIndex < 0)
                    break;

                vidTitle[startIndex] = ' ';
            }
            return new string(vidTitle);
        }
    }
}