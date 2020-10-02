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
            Channels = new StreamlinkChannelConfig[] {};
        }

        public string StreamlinkPath { get; set; }
        public string StreamQuality { get; set; }
        public string ExtraArguments { get; set; }
        public StreamlinkChannelConfig[] Channels { get; set; }
    }

    public class StreamlinkChannelConfig
    {
        public string Name { get; set; }
        public string Image { get; set; }
        public string Url { get; set; }
    }
}
