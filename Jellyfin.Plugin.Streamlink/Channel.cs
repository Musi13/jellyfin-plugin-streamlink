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
using MediaBrowser.Model.Dto;
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

        private string GetChannelId(StreamlinkChannelConfig channel)
        {
            return channel.Url.GetMD5().ToString("N", CultureInfo.InvariantCulture);
        }

        public Task<ChannelItemResult> GetChannelItems(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            return GetChannelItemsInternal(cancellationToken);
        }


        private Task<ChannelItemResult> GetChannelItemsInternal(CancellationToken cancellationToken)
        {
            var items = new List<ChannelItemInfo>();

            foreach (var s in Plugin.Instance.Configuration.Channels)
            {
                var item = new ChannelItemInfo
                {
                    Name = s.Name,
                    ImageUrl = s.Image,
                    Id = GetChannelId(s),
                    Type = ChannelItemType.Media,
                    ContentType = ChannelMediaContentType.Clip,
                    MediaType = ChannelMediaType.Video,
                    IsLiveStream = true,

                    MediaSources = new List<MediaSourceInfo>
                    {
                        CreateMediaSourceInfo(s)
                    }
                };

                items.Add(item);
            }

            return Task.FromResult(new ChannelItemResult
            {
                Items = items
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

        protected virtual MediaSourceInfo CreateMediaSourceInfo(StreamlinkChannelConfig channel)
        {
            var mediaSource = new MediaSourceInfo
            {
                Path = channel.Url,
                Protocol = MediaProtocol.File,
                MediaStreams = new List<MediaStream>
                {
                    new MediaStream
                    {
                        Type = MediaStreamType.Video,
                        // Set the index to -1 because we don't know the exact index of the video stream within the container
                        // For twitch streams it seems to be 1, but that might not be consistent
                        Index = -1,
                        IsInterlaced = true
                    },
                    new MediaStream
                    {
                        Type = MediaStreamType.Audio,
                        // Set the index to -1 because we don't know the exact index of the audio stream within the container
                        // For twitch streams it seems to be 0
                        Index = -1
                    }
                },
                RequiresOpening = true,
                RequiresClosing = true,
                RequiresLooping = false,

                OpenToken = StreamlinkProvider.Prefix + GetChannelId(channel),

                ReadAtNativeFramerate = false,

                Id = GetChannelId(channel),
                IsInfiniteStream = true,
                IsRemote = true,

                IgnoreDts = true,
                SupportsDirectPlay = true,
                SupportsDirectStream = true
            };

            mediaSource.InferTotalBitrate();

            return mediaSource;
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
