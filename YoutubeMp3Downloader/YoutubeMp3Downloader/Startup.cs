using Microsoft.Owin;
using Owin;
using YoutubeMp3Downloader;

namespace YoutubeMp3Downloader
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.MapSignalR();
        }
    }
}