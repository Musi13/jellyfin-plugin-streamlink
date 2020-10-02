using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Logging;
using Jellyfin.Plugin.Streamlink;
using Jellyfin.Plugin.Streamlink.Configuration;

using Emby.Server.Implementations.LiveTv.TunerHosts;

namespace Jellyfin.Plugin.Streamlink
{
    public class StreamlinkStream : LiveStream, IDirectStreamProvider
    {
        private readonly IServerApplicationHost _appHost;
        private readonly IConfigurationManager _configurationManager;

        public StreamlinkStream(
            MediaSourceInfo mediaSource,
            TunerHostInfo tunerHostInfo,
            string originalStreamId,
            IFileSystem fileSystem,
            ILogger logger,
            IConfigurationManager configurationManager,
            IServerApplicationHost appHost,
            IStreamHelper streamHelper)
            : base(mediaSource, tunerHostInfo, fileSystem, logger, configurationManager, streamHelper)
        {
            _appHost = appHost;
            _configurationManager = configurationManager;
            OriginalStreamId = originalStreamId;
            EnableStreamSharing = true;
        }

        public override async Task Open(CancellationToken openCancellationToken)
        {
            LiveStreamCancellationTokenSource.Token.ThrowIfCancellationRequested();

            var mediaSource = OriginalMediaSource;

            var url = mediaSource.Path;

            var encodingConfig = _configurationManager.GetConfiguration<EncodingOptions>("encoding");

            Directory.CreateDirectory(Path.GetDirectoryName(TempFilePath));

            var typeName = GetType().Name;
            Logger.LogInformation("Opening {0} Live stream from {1}", typeName, url);


            var streamlinkProc = new Process();
            streamlinkProc.StartInfo.FileName = Jellyfin.Plugin.Streamlink.Plugin.Instance.Configuration.StreamlinkPath;
            streamlinkProc.StartInfo.UseShellExecute = false;
            streamlinkProc.StartInfo.RedirectStandardOutput = true;
            streamlinkProc.StartInfo.ArgumentList.Add("--ffmpeg-ffmpeg=" + encodingConfig.EncoderAppPath);
            streamlinkProc.StartInfo.ArgumentList.Add("--quiet");
            streamlinkProc.StartInfo.ArgumentList.Add("--stdout");
            foreach (string arg in Jellyfin.Plugin.Streamlink.Plugin.Instance.Configuration.ExtraArguments.Split(" "))
                if (!string.IsNullOrWhiteSpace(arg))
                    streamlinkProc.StartInfo.ArgumentList.Add(arg);
            streamlinkProc.StartInfo.ArgumentList.Add(url);
            streamlinkProc.StartInfo.ArgumentList.Add(Jellyfin.Plugin.Streamlink.Plugin.Instance.Configuration.StreamQuality);

            Logger.LogInformation(
                "Starting streamlink with: {0} {1}", 
                streamlinkProc.StartInfo.FileName,
                String.Join(" ", streamlinkProc.StartInfo.ArgumentList)
            );

            SetTempFilePath("ts");

            var taskCompletionSource = new TaskCompletionSource<bool>();

            var now = DateTime.UtcNow;

            #pragma warning disable CS4014
            StartStreaming(streamlinkProc, taskCompletionSource, LiveStreamCancellationTokenSource.Token);

            MediaSource.Path = _appHost.GetLoopbackHttpApiUrl() + "/LiveTv/LiveStreamFiles/" + UniqueId + "/stream.ts";
            MediaSource.Protocol = MediaProtocol.Http;
            MediaSource.LiveStreamId = OriginalStreamId;

            await taskCompletionSource.Task.ConfigureAwait(false);
            if (taskCompletionSource.Task.Exception != null)
            {
                // Error happened while opening the stream so raise the exception again to inform the caller
                throw taskCompletionSource.Task.Exception;
            }

            if (!taskCompletionSource.Task.Result)
            {
                Logger.LogWarning("Zero bytes copied from stream {0} to {1} but no exception raised", GetType().Name, TempFilePath);
                throw new EndOfStreamException(String.Format(CultureInfo.InvariantCulture, "Zero bytes copied from stream {0}", GetType().Name));
            }
        }

        private Task StartStreaming(Process streamlinkProc, TaskCompletionSource<bool> openTaskCompletionSource, CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                try
                {
                    Logger.LogInformation("Beginning {0} stream to {1}", GetType().Name, TempFilePath);
                    streamlinkProc.Start();
                    using (streamlinkProc)
                    using (var stream = streamlinkProc.StandardOutput.BaseStream)
                    using (var fileStream = new FileStream(TempFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
                    {
                        await StreamHelper.CopyToAsync(
                            stream,
                            fileStream,
                            IODefaults.CopyToBufferSize,
                            () => Resolve(openTaskCompletionSource),
                            cancellationToken).ConfigureAwait(false);

                    }
                }
                catch (OperationCanceledException ex)
                {
                    Logger.LogInformation("Copying of {0} to {1} was canceled", GetType().Name, TempFilePath);
                    openTaskCompletionSource.TrySetException(ex);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error copying live stream {0} to {1}.", GetType().Name, TempFilePath);
                    openTaskCompletionSource.TrySetException(ex);
                }
                finally
                {
                    streamlinkProc.Kill();
                    streamlinkProc.WaitForExit();
                }

                openTaskCompletionSource.TrySetResult(false);

                EnableStreamSharing = false;
                await DeleteTempFiles(new List<string> { TempFilePath }).ConfigureAwait(false);
            });
        }

        private void Resolve(TaskCompletionSource<bool> openTaskCompletionSource)
        {
            DateOpened = DateTime.UtcNow;
            openTaskCompletionSource.TrySetResult(true);
        }
    }
}
