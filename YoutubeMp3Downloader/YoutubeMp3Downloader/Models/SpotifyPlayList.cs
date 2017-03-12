using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace YoutubeMp3Downloader.Models
{
    public class SpotifyPlayList
    {
        public string playListId { get; set; }

        public long userId { get; set; }

        public string name { get; set; }

        public string token { get; set; }
    }
}