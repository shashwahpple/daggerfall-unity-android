![image](https://github.com/Interkarma/daggerfall-unity/assets/10426244/4f176f9d-6332-47b3-a4d7-317ed8d6b38b)

# Unofficial Android Port of Daggerfall Unity

This is an Android port of the original DFU project found at https://github.com/Interkarma/daggerfall-unity, itself built on top of the general-purpose Android port at [Vwing/daggerfall-unity-android](https://github.com/Vwing/daggerfall-unity-android).

Please visit [Vwing's releases page](https://github.com/Vwing/daggerfall-unity-android/releases) if you're looking for a general-purpose Android APK or setup instructions. If you have any questions or feedback, or if you want access to the bleeding-edge pre-release builds, please join the [#dfu-android](https://discord.com/channels/559842137374457876/1248079754641277059) channel of Lysandus' Tomb Discord ([invite link](https://discord.gg/rn95kxPGpg)).

Feature requests or bugs related to the general Android port should be opened as an issue on [Vwing's fork](https://github.com/Vwing/daggerfall-unity-android/issues), *not* here or on the main project. Issues specific to this fork's dual-screen/gamepad handheld work belong on [this fork's issue tracker](https://github.com/shashwahpple/daggerfall-unity-android/issues) instead. Alternatively, you may open a post on the Discord in the [#android-feedback](https://discord.com/channels/559842137374457876/1257519041103396924) channel.

## Goal of This Fork

Vwing's port targets Android in general (phones and tablets); this fork specializes it for **Android dual-screen/gamepad handhelds**, such as the [AYN Thor](https://www.ayntec.com/), which pair a second physical display with built-in controller buttons and sticks. The goal is to make Daggerfall Unity feel native to that form factor, rather than a phone port with a virtual joystick overlay. Work so far includes:

- **Second-screen UI** — a persistent panel set on the device's second display, independent of the main 3D view: an interaction-mode switcher, a tap-to-equip inventory list, and a visual Paper Doll (equipped-gear character view, tap an item to unequip it).
- **Physical gamepad support** — an opt-in default control scheme mapped to the AYN Thor's face buttons, bumpers/triggers, and sticks, built on top of Daggerfall Unity's existing (keyboard/mouse-oriented) input binding system.
- **Android build fixes** — e.g. ensuring Addressables content is actually built into APKs produced by the Multi-Build Tool.

There's no published release yet for this fork specifically - build it yourself via **Daggerfall Tools → Android → Multi-Build Tool** in the Unity Editor and install the resulting APK on your device.

## AI Assistance Disclaimer

I'm a software developer by trade, but my specialization is business and software integration - not Android platform engineering, Unity internals, or game input architecture. The Android-specific work in this fork (second-screen UI, gamepad input mapping, build pipeline fixes) was developed with substantial assistance from Claude Code (Anthropic's AI coding assistant), including investigating Daggerfall Unity's existing systems, implementing changes, and on-device debugging. I've reviewed and tested everything on my own hardware, but wanted to be upfront about the extent of AI involvement given this is outside my usual area of expertise.

# What is Daggerfall Unity?

Daggerfall Unity is an open source recreation of Daggerfall in the Unity engine created by [Daggerfall Workshop](http://www.dfworkshop.net).

Experience the adventure and intrigue of Daggerfall with all of its original charm along with hundreds of fixes, quality of life enhancements, and extensive mod support.

## Classic Daggerfall Plus

+ Cross-platform without emulation (Windows/Linux/Mac)
+ Retro graphics are boosted by modern engine and lighting
+ High resolution widescreen with classic style
+ Optionally play in retro mode 320x200 or 640x400 with VGA palettes
+ Optionally overhaul the graphics and gameplay with mods
+ Huge draw distances even without mods
+ Smooth first-person controls
+ Quality of life enhancements
+ Extensive mod support with an active creator community
+ Translation support via community mods

# Get Daggerfall Unity

Daggerfall Unity requires a free copy of DOS Daggerfall to run. This provides all necessary game assets such as textures, 3D models, and sound effects.

You can get a free copy of DOS Daggerfall from [Steam](https://store.steampowered.com/app/1812390/The_Elder_Scrolls_II_Daggerfall/) and a free copy of Daggerfall Unity from the [Releases](https://github.com/Interkarma/daggerfall-unity/releases) page. Then simply unzip the latest version of Daggerfall Unity to its own folder and point it to the DOS version. Daggerfall Unity will take care of everything else.

Here are a couple of links with more detailed steps to help you get started using either Steam or a cross-platform process.

+ [Using Steam Release of Daggerfall with Daggerfall Unity](https://github.com/Interkarma/daggerfall-unity/wiki/Using-Steam-Release-of-Daggerfall-with-Daggerfall-Unity)
+ [Installing Daggerfall Unity Cross Platform](https://github.com/Interkarma/daggerfall-unity/wiki/Installing-Daggerfall-Unity-Cross-Platform)

# System Requirements

Daggerfall Unity has the following system requirements. Please note that optional mods may substantially increase system requirements or cause game to become less stable.

### Minimum
* Operating system: Windows, Linux, MacOS
* Processor: Intel i3 (Skylake) equivalent
* Graphics: DirectX 11 capable with 1GB video memory and up-to-date drivers
* Memory: 2GB system RAM

### Recommended
* Operating system: Windows, Linux, MacOS
* Processor: Intel i5 (Skylake) equivalent
* Graphics: GTX 660 with 2GB video memory and up-to-date drivers
* Memory: 4GB system RAM

# Featured Mods

Daggerfall Unity has an active mod community with hundreds of incredible mods. It's impossible to feature them all, but here's a sampling of a few popular mods that represent a variety of mods the community has created. They range from graphical overhauls, to new quests, new guilds, adding new world areas, and changing game formulas and other behaviours.

## DREAM

![image](https://github.com/Interkarma/daggerfall-unity/assets/10426244/6a424f3c-f7f1-4def-82e9-98b6486dfc21)

The Daggerfall Remaster Enchanted Art Mod (DREAM) upgrades game assets including sound, music, videos & all graphics found in the game. It goes beyond a restoration and additionally fixes the old quirks, bugs and increases variety/fidelity everywhere possible.

[Link to DREAM on Nexus](https://www.nexusmods.com/daggerfallunity/mods/5)

## Quest Pack 1

![image](https://github.com/Interkarma/daggerfall-unity/assets/10426244/43688bd8-c3b0-42b5-bb64-066d6867ff66)

This quest pack offers 195 original new quests for Daggerfall Unity, mostly for the game's guilds.

[Link to Quest Pack 1 on Nexus](https://www.nexusmods.com/daggerfallunity/mods/2)

## Archaeologists

![image](https://github.com/Interkarma/daggerfall-unity/assets/10426244/282aa3a9-1662-49d9-9319-a9358775ea33)

Enhance Daggerfall Unity gameplay by making language skills more viable and adding a new guild to the game, called "The Archaeologists Guild". Their mission is to delve into the history of Tamriel and they're interested in all creatures and races who have lived or still live there. Joining gives access to locator devices to aid in dungeon delving.

[Link to Archaeologists on Nexus](https://www.nexusmods.com/daggerfallunity/mods/14)

## World of Daggerfall Project

![image](https://github.com/Interkarma/daggerfall-unity/assets/10426244/c3909d99-9ce4-4c41-8aaf-ee7dce49a6d7)

From the ashes of Daggerfall's past, experience the world of Daggerfall as originally envisioned. With glorious mountain tops that reach for the heavens, and countless new locations to explore.

[Link to World of Daggerfall Project on Nexus](https://www.nexusmods.com/daggerfallunity/mods/249)

## Finding My Religion

![image](https://github.com/Interkarma/daggerfall-unity/assets/10426244/995ae53d-eb99-451f-942c-931dd31dc85c)

Finding My Religion is a multi-release visual and gameplay overhaul of Daggerfall's religions. The current release is "Detailed Temples", which decorates and redesigns the temples' according to the worshipped deity's sphere of influence. Julianos boasts a bigger library than others, Kynareth has an indoor garden, Mara has a birthing room and more. Each temple have been expanded downwards,  with priests' quarters and additional rooms for all service members. They also feature a crypt where people of importance have been buried.

[Link to Finding My Religion on Nexus](https://www.nexusmods.com/daggerfallunity/mods/344)

## Physical Combat And Armor Overhaul

![image](https://github.com/Interkarma/daggerfall-unity/assets/10426244/35bc2fb2-5b3d-401d-b3cb-806a59241014)

Instead of increasing chance to avoid an attack completely, armor now reduces the damage you take, based on the material as well as the type of attack. Skills now determine most of your chance to avoid attacks, including many more features.

[Link to Physical Combat And Armor Overhaul on Nexus](https://www.nexusmods.com/daggerfallunity/mods/76)

## Links

+ [Daggerfall Unity Nexus](https://www.nexusmods.com/daggerfallunity) - *Discover more mods for Daggerfall Unity*
+ [Lysandus' Tomb Discord](https://discord.gg/rn95kxPGpg) - *Join an active growing community*
+ [Daggerfall Workshop](http://www.dfworkshop.net/) - *Follow the development of Daggerfall Unity*
+ [Workshop Forums](http://forums.dfworkshop.net/) - *Join the forum community*
+ [Reddit](https://www.reddit.com/r/daggerfallunity) - *Join the Daggerfall Unity subreddit*
+ [Twitter](https://twitter.com/gav_clayton) - *Follow lead developer on Twitter for more news*
