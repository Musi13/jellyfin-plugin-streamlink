<h1 align="center">Jellyfin Streamlink Plugin</h1>
<h3 align="center">A plugin for the <a href="https://jellyfin.media">Jellyfin Project</a></h3>
<h3 align="center">to work with <a href="https://streamlink.github.io">Streamlink</a></h3>

<p align="center">
This plugin is built with .NET Core to use Streamlink as a channel provider for Jellyfin.
</p>

## Some Notes About This Project

First and foremost, this plugin is not an official plugin adopted by the Jellyfin project. Do not bother the Jellyfin dev team for any issues related to it.

This is a plugin to watch various livestream sites through Jellyfin. Generally I would advise against using this and instead using those sites' apps directly, since they will be more maintained. The initial work was done on this as a weekend project and the motivation was that certain livestreaming platforms don't have first party (or even third party) apps on certain devices (e.g. Amazon pulled all Twitch apps from Roku).

This plugin uses Jellyfin like a proxy to those services, provided that Streamlink supports the streaming platform and Jellyfin has a client on the target device.

At the time of writing, Jellyfin is going through some rather large internal refactoring, which means this plugin may or may not work for very long. Additionally, there are some issues with creating a live streaming plugin; basically the internals of Jellyfin aren't very modular in this area (yet). This means that to build this project, it relies on some concrete implementations in the older Emby codebase (though this is fairly irrelevant to end users). For various reasons, I've chosen not to request this plugin be adopted or featured as "official", at least not until I have more time to maintain it. If anyone wants to maintain this plugin beyond basic functionality, feel free to fork it and even talk with Jellyfin team yourself.

In any case, there are some features that I would like to add before aiming at a "wide release": theres no indication that a stream is offline (loading just hangs), there's not a lot of error reporting through the Web UI, and the configuration for adding a stream could be better. I'd also like to have better testing on Jellyfin's supported platforms, currently I only run this on Linux/Ubuntu 18.04.


## Build Process

1. Clone or download this repository

2. Ensure you have .NET Core 3.1+ SDK setup and installed

3. Copy Emby.Server.Implementations.dll from an existing Jellyfin install into the repo root

4. Build plugin with following command.

```sh
dotnet publish --configuration Release --output bin
```

## Installation Process

1. Place the dll file in the `plugins` folder under the program data directory or inside the portable install directory. E.g. .../plugins/Streamlink/Jellyfin.Plugin.Streamlink.dll

2. Restart jellyfin

3. Log in through the Web UI and change the plugin settings to point to your installation of Streamlink and add streams.
