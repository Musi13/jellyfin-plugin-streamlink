using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Dto;
using Microsoft.Extensions.Logging;

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Serialization;
using Microsoft.Net.Http.Headers;

using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Entities;

using Jellyfin.Plugin.Streamlink;

namespace MediaBrowser.Providers.Plugin.Streamlink
{
    public class StreamlinkProvider : IMediaSourceProvider
    {
        private readonly IServerConfigurationManager _config;
        private readonly ILogger<StreamlinkProvider> _logger;
        private readonly IFileSystem _fileSystem;
        private readonly IServerApplicationHost _appHost;
        private readonly IStreamHelper _streamHelper;

        public static string Prefix => typeof(StreamlinkProvider).FullName.GetMD5().ToString("N", CultureInfo.InvariantCulture) + "_";

        public StreamlinkProvider(
            IServerConfigurationManager config,
            ILogger<StreamlinkProvider> logger,
            IFileSystem fileSystem,
            IServerApplicationHost appHost,
            IStreamHelper streamHelper
        )
        {
            _config = config;
            _logger = logger;
            _fileSystem = fileSystem;
            _appHost = appHost;
            _streamHelper = streamHelper;
        }

        /// <summary>
        /// Gets the media sources.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task&lt;IEnumerable&lt;MediaSourceInfo&gt;&gt;.</returns>
        public Task<IEnumerable<MediaSourceInfo>> GetMediaSources(BaseItem item, CancellationToken cancellationToken)
        {
            var ctask = (new MediaBrowser.Channels.Streamlink.Channel()).GetChannelItems(null, cancellationToken);
            ctask.Wait();
            var channelInfos = ctask.Result.Items;
            foreach (var channel in channelInfos)
            {
                if (channel.MediaSources[0].Path == item.Path)
                {
                    _logger.LogDebug("Found StreamlinkChannel for: " + item.Path);
                    return Task.FromResult(channel.MediaSources as IEnumerable<MediaSourceInfo>);
                }
            }

            _logger.LogDebug("Couldn't find channel for: " + item.Path);
            return Task.FromResult(Enumerable.Empty<MediaSourceInfo>());
        }

        /// <summary>
        /// Opens the media source.
        /// </summary>
        public Task<ILiveStream> OpenMediaSource(string openToken, List<ILiveStream> currentLiveStreams, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Getting streamlink channel stream for {0}", openToken);

            _logger.LogDebug("Current Streams: {0}", currentLiveStreams);

            var ctask = (new MediaBrowser.Channels.Streamlink.Channel()).GetChannelItems(null, cancellationToken);
            ctask.Wait();
            var channelInfos = ctask.Result.Items;
            foreach (var channel in channelInfos)
            {
                if (channel.Id == openToken)
                {
                    var mediaSource = channel.MediaSources[0];
                    var stream = new StreamlinkStream(mediaSource, null, channel.Id, _fileSystem, _logger, _config, _appHost, _streamHelper) as ILiveStream;
                    stream.Open(cancellationToken);
                    return Task.FromResult(stream);
                }
            }

            throw new Exception("Couldn't find channel for given openToken: " + openToken);
        }
    }
}
