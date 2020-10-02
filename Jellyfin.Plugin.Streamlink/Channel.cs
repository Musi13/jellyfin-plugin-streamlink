using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Common.Extensions;

using Jellyfin.Plugin.Streamlink;
using Jellyfin.Plugin.Streamlink.Configuration;
using MediaBrowser.Providers.Plugin.Streamlink;

namespace MediaBrowser.Channels.Streamlink
{
    public class Channel : IChannel, IHasCacheKey
    {
        public Channel() {}

        public string HomePageUrl => string.Empty;

        public virtual string Name => "Streamlink";

        // Increment as needed to invalidate all caches
        public string DataVersion => "1";

        public Task<ChannelItemResult> GetChannelItems(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            return Task.FromResult(new ChannelItemResult {
                Items = (from c in Plugin.Instance.Configuration.Channels select c.CreateChannelItemInfo()).ToList<ChannelItemInfo>()
            });
        }

        public IEnumerable<ImageType> GetSupportedChannelImages()
        {
            return new List<ImageType>
            {
                ImageType.Thumb,
                ImageType.Backdrop
            };
        }

        public InternalChannelFeatures GetChannelFeatures()
        {
            return new InternalChannelFeatures
            {
                ContentTypes = new List<ChannelMediaContentType>
                {
                    ChannelMediaContentType.Clip
                },

                MediaTypes = new List<ChannelMediaType>
                {
                    ChannelMediaType.Video
                },

                SupportsContentDownloading = true
            };
        }

        public Task<DynamicImageResponse> GetChannelImage(ImageType type, CancellationToken cancellationToken)
        {
            switch (type)
            {
                case ImageType.Thumb:
                case ImageType.Backdrop:
                    {
                        var path = GetType().Namespace + ".Images." + type.ToString().ToLowerInvariant() + ".png";

                        return Task.FromResult(new DynamicImageResponse
                        {
                            Format = ImageFormat.Png,
                            HasImage = true,
                            Stream = typeof(Channel).Assembly.GetManifestResourceStream(path)
                        });
                    }
                default:
                    throw new ArgumentException("Unsupported image type: " + type);
            }
        }

        public bool IsEnabledFor(string userId)
        {
            return true;
        }

        public ChannelParentalRating ParentalRating
            => ChannelParentalRating.GeneralAudience;

        public string GetCacheKey(string userId)
            => Guid.NewGuid().ToString("N");

        public string Description => string.Empty;
    }
}
