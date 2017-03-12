using Microsoft.AspNet.SignalR;
using Microsoft.Owin;
using Owin;
using YoutubeMp3Downloader;

namespace YoutubeMp3Downloader
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var hubConfiguration = new HubConfiguration();
            hubConfiguration.EnableDetailedErrors = true;
            app.MapSignalR(hubConfiguration);
            //app.MapSignalR();
        }
    }
}