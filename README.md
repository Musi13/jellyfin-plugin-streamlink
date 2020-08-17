<h1 align="center">Jellyfin Streamlink Plugin</h1>
<h3 align="center">Part of the <a href="https://jellyfin.media">Jellyfin Project</a></h3>
<h3 align="center">A plugin to work with <a href="https://streamlink.github.io">Streamlink</a></h3>

<p align="center">
This plugin is built with .NET Core to use Streamlink as a tuner for Jellyfin's LiveTV service.
</p>

## Some Notes About This Project

First and foremost, this plugin is not an official plugin adopted by the Jellyfin project. Do not bother the Jellyfin dev team for any issues related to it.

This is a plugin to watch various livestream sites through Jellyfin. Generally I would advise against using this and instead using those sites' apps directly, since they will be more maintained. The initial work was done on this as a weekend project and the motivation was that certain livestreaming platforms don't have first party (or even third party) apps on certain devices (e.g. Amazon pulled all Twitch apps from Roku).

This plugin uses Jellyfin like a proxy to those services, provided that Streamlink supports the streaming platform and Jellyfin has a client on the target device.

At the time of writing, Jellyfin is going through some rather large internal refactoring, which means this plugin may or may not work for very long. Additionally, there are some issues with creating a LiveTV plugin; basically the internals of Jellyfin aren't very modular in this area (yet). This means that to build this project, it relies on some concrete implementations in the older Emby codebase, and to actually use it requires some manual changes to the Jellyfin server's Web UI. Since these pieces will likely change while the Jellyfin team cleans up the code, and I don't want to make the LiveTV implementation more modular myself, I've chosen not to request this plugin be adopted or featured as "official". If anyone wants to maintain this plugin beyond basic functionality, feel free to fork it and even talk with Jellyfin team yourself.

In any case, there are some features that I would like to add before aiming at a "wide release": theres no indication that a stream is offline (loading just hangs), there's not a lot of error reporting through the Web UI, and the configuration for adding a stream could be more clear. For clarification, it uses the url/local file picker that the M3U tuner uses, even though local files don't make sense in this case (this is to minimize the manual web ui changes).


## Build Process

1. Clone or download this repository

2. Ensure you have .NET Core 3.1+ SDK setup and installed

3. Copy Emby.Server.Implementations.dll from an existing Jellyfin install into the repo root

4. Build plugin with following command.

```sh
dotnet publish --configuration Release --output bin
```

## Installation Process

1. Place the dll file in the `plugins` folder under the program data directory or inside the portable install directory. E.g. .../plugins/Streamlink/Jellyfin.Plufin.Streamlink.dll

2. Manually apply this diff to the jellyfin-web install (e.g. /usr/lib/jellyfin/bin/jellyfin-web):
```
diff --git a/src/components/tunerpicker.js b/src/components/tunerpicker.js
index 4dd5ecd3d..0c7f27ca6 100644
--- a/src/components/tunerpicker.js
+++ b/src/components/tunerpicker.js
@@ -69,6 +69,9 @@ define(["dialogHelper", "dom", "layoutManager", "connectionManager", "globalize"
             case "satip":
                 return "DVB";
 
+            case "streamlink":
+                return "streamlink";
+
             default:
                 return "Unknown";
         }
diff --git a/src/controllers/livetvstatus.js b/src/controllers/livetvstatus.js
index aee5876a4..17bbe3cbc 100644
--- a/src/controllers/livetvstatus.js
+++ b/src/controllers/livetvstatus.js
@@ -193,6 +193,8 @@ define(["jQuery", "globalize", "scripts/taskbutton", "dom", "libraryMenu", "layo
                 return "Hauppauge";
             case "satip":
                 return "DVB";
+            case "streamlink":
+                return "streamlink";
             default:
                 return "Unknown";
         }
diff --git a/src/controllers/livetvtuner.js b/src/controllers/livetvtuner.js
index 55a86d4be..741ac759f 100644
--- a/src/controllers/livetvtuner.js
+++ b/src/controllers/livetvtuner.js
@@ -120,7 +120,7 @@ define(["globalize", "loading", "libraryMenu", "dom", "emby-input", "emby-button
         var supportsTranscoding = "hdhomerun" === value;
         var supportsFavorites = "hdhomerun" === value;
         var supportsTunerIpAddress = "hdhomerun" === value;
-        var supportsTunerFileOrUrl = "m3u" === value;
+        var supportsTunerFileOrUrl = "m3u" === value || "streamlink" === value;
         var supportsStreamLooping = "m3u" === value;
         var supportsTunerCount = "m3u" === value;
         var supportsUserAgent = "m3u" === value;
```
This is to fix the LiveTV Tuner settings, so you can actually configure the plugin. Note that updating Jellyfin will undo these changes, but the settings will be retained; you don't have to redo this unless you want to change the settings again.

3. Restart jellyfin

4. Log in through the Web UI and change the plugin settings to point to your installation of Streamlink