using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Emby.Server.Implementations.LiveTv.TunerHosts
{
    public class StreamlinkTunerHost : BaseTunerHost, ITunerHost, IConfigurableTunerHost
    {
        private readonly IServerApplicationHost _appHost;
        private readonly IMediaSourceManager _mediaSourceManager;
        private readonly IStreamHelper _streamHelper;

        public StreamlinkTunerHost(
            IServerConfigurationManager config,
            IMediaSourceManager mediaSourceManager,
            ILogger<M3UTunerHost> logger,
            IJsonSerializer jsonSerializer,
            IFileSystem fileSystem,
            IServerApplicationHost appHost,
            IStreamHelper streamHelper
        ) : base(config, logger, jsonSerializer, fileSystem)
        {
            _appHost = appHost;
            _mediaSourceManager = mediaSourceManager;
            _streamHelper = streamHelper;
        }

        public override string Type => "streamlink";

        public virtual string Name => "Streamlink Tuner";

        private string GetFullChannelIdPrefix(TunerHostInfo info)
        {
            return ChannelIdPrefix + info.Url.GetMD5().ToString("N", CultureInfo.InvariantCulture);
        }

        protected override async Task<List<ChannelInfo>> GetChannelsInternal(TunerHostInfo info, CancellationToken cancellationToken)
        {
            return new List<ChannelInfo>{await GetChannelnfo(info, cancellationToken).ConfigureAwait(false)};
        }

        public Task<List<LiveTvTunerInfo>> GetTunerInfos(CancellationToken cancellationToken)
        {
            var list = GetTunerHosts()
            .Select(i => new LiveTvTunerInfo()
            {
                Name = Name,
                SourceType = Type,
                Status = LiveTvTunerStatus.LiveTv,
                Id = GetFullChannelIdPrefix(i),
                Url = i.Url
            })
            .ToList();
            return Task.FromResult(list);
        }

        private async Task<ChannelInfo> GetChannelnfo(TunerHostInfo info, CancellationToken cancellationToken)
         {
             return new ChannelInfo()
             {
                 TunerHostId = info.Id,
                 Name = info.Url,
                 Path = info.Url,
                 Id = GetFullChannelIdPrefix(info),
             };
         }

        protected override async Task<ILiveStream> GetChannelStream(TunerHostInfo info, ChannelInfo channelInfo, string streamId, List<ILiveStream> currentLiveStreams, CancellationToken cancellationToken)
        {
            Logger.LogDebug("Getting streamlink channel stream for {0}", info.Url);
            var mediaSource = CreateMediaSourceInfo(info, channelInfo);
            return new StreamlinkStream(mediaSource, info, streamId, FileSystem, Logger, Config, _appHost, _streamHelper);
        }

        public async Task Validate(TunerHostInfo info)
        {
            // TODO: Check that info.Url can be handled, e.g. `streamlink --can-handle-url $info.Url` exits 0
            return;
        }

        protected override Task<List<MediaSourceInfo>> GetChannelStreamMediaSources(TunerHostInfo info, ChannelInfo channelInfo, CancellationToken cancellationToken)
        {
            return Task.FromResult(new List<MediaSourceInfo> { CreateMediaSourceInfo(info, channelInfo) });
        }

        protected virtual MediaSourceInfo CreateMediaSourceInfo(TunerHostInfo info, ChannelInfo channel)
        {
            var mediaSource = new MediaSourceInfo
            {
                Path = info.Url,
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

                ReadAtNativeFramerate = false,

                Id = GetFullChannelIdPrefix(info),
                IsInfiniteStream = true,
                IsRemote = true,

                IgnoreDts = true,
                SupportsDirectPlay = true,
                SupportsDirectStream = true
            };

            mediaSource.InferTotalBitrate();

            return mediaSource;
        }

        public Task<List<TunerHostInfo>> DiscoverDevices(int discoveryDurationMs, CancellationToken cancellationToken)
        {
            return Task.FromResult(new List<TunerHostInfo>());
        }
    }
}