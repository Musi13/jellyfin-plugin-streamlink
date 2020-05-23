using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Streamlink.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public PluginConfiguration()
        {
            StreamlinkPath = null;
            StreamQuality = "best";
            ExtraArguments = "";
        }

        public string StreamlinkPath { get; set; }
        public string StreamQuality { get; set; }
        public string ExtraArguments { get; set; }
    }
}
