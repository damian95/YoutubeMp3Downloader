using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace YoutubeMp3Downloader.Models
{
    public class YoutubeVid
    {
        public int id { get; set; }

        public string title { get; set; }

        public string duration { get; set; }

        public string description { get; set; }

        public string uploader { get; set; }

        public string url { get; set; }

        public string imgLink { get; set; }
    }
}