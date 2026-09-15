# OWMixedInput

An input system rewrite for Outer Wilds.

## WARNING

This mod has not been thoroughly play tested. While I am reasonably confident in the quality of my code, expect bugs.

## Features

- Simultaneous keyboard, mouse, and gamepad inputs.
- Independent control scheme and UI settings. No flickering UI.
- More responsive mouse inputs with optional mouse smoothing.
- Gyro camera with flick stick and camera reset.
- Square joystick inputs for more responsive thrusters.
- Many, many settings for players with particular preferences. 
Play your way.

See [SETTINGS.md](SETTINGS.md) for a detailed description of each of the settings.

See [DETAILS.md](DETAILS.md) for a detailed description of all behavior changes.

## Installation

TODO

## Build Instructions

You should be able to build the mod yourself by cloning the repo and running `dotnet build -c Release` from the project root.
If you have issues, you may need to tweak `OWMixedInput.csproj.user` to point to your OWML install.

The `Settings.Generated.cs` source file is auto-generated from the information in `default-config.json` with `Generate_Settings_cs.py`.
This script requires `python3` and is run automatically by `dotnet build` when `Settings.Generated.cs` is out of date.
I have no clue if the way I have integrated `phyton3` into the build system works on Windows, but if you don't touch `default-config.json`, you shouldn't need to run `python3`.

## Mod Compatibility

Although I made a reasonable attempt at compatibility with other mods, universal compatibility is not feasible. 
Any mod which changes the behavior of the input system, the camera system, or the thruster system is likely to be incompatible.

### Known compatible mods

- [Vanilla Fix by JohnCorby](https://outerwildsmods.com/mods/vanillafix/)
- [Time Saver by Bwc9876](https://outerwildsmods.com/mods/timesaver/)
- [Secret Settings by xen](https://outerwildsmods.com/mods/secretsettings/)

These are the only mods I am currently using. 
Future updates to these mods could break compatibility, but I expect this will not happen.

### Known incompatible mods

- [Mouse Fix by Novaenia](https://outerwildsmods.com/mods/mousefix/). However, my mod encompasses the behavior of Mouse Fix so long as you turn off mouse smoothing. If you only care about mouse responsiveness, you should use Novaenia's mod instead of mine.

Feel free to file an issue/PR if you think a mod deserves inclusion in either list or would like to request a compatibility patch for a mod.

Some of the API surface of the vanilla game (e.g. `OWInput.UsingGamepad`) does not make sense in a context where any set of input methods can be in use simultaneously.
These APIs have been modified or deprecated accordingly.
Mod developers should refer to [DETAILS.md](DETAILS.md) for details regarding how the input APIs have changed and what patches have been applied to which methods.

## Controller Compatibility

All controllers that function in the vanilla game should function as usual with this mod installed.

Gryo controls are implemented via [SDL3](https://wiki.libsdl.org/SDL3/FrontPage), so any controller with gyro inputs supported by SDL3 should be supported by this mod.

### Gyro Compatibility

- **DualSense (PS5)**: Works. This is what I use.
- **DualShock 4 (PS4)**: Untested. Probably works. I don't have one of these. 
- **Steam Controller/Steam Deck**: Untested. No clue if it will work. I don't have either of these.
- **Switch Pro Controller**: Does not work on my machine.
I have no clue why.
I suspect it is an issue with SDL3 and Wine/Proton. 
If anyone can test this on a Windows machine or let me know if I am doing something wrong, please file an issue/PR.

### Gyro and Linux

SDL3 uses the `hidraw` API to obtain gyro inputs.
Usually, you will need `udev` rules to make sure your user has access to the relevant `hidraw` device.

See [ValvaSoftware/steam-devices](https://github.com/ValveSoftware/steam-devices) or [fabiscafe/game-devices-udev](https://github.com/fabiscafe/game-devices-udev) for more information.

## Contributing

I welcome issues and PRs. 

I will not accept code style changes (unless I have done something egregiously bad).
I know that I am not using all of the usual C# conventions, but I find my conventions more readable.

Any issues/PRs made by an autonomous agent will be closed/rejected without explanation.

## Future Possibilities

I am open to adding more features in the future. 
If you have requests, please file an issue.

Here is a list of the features I am considering implementing:
- Gyro thruster controls.
However, I don't have a good idea for how gyro inputs are supposed to work with 'realistic' thruster simulation.
If you have an idea or know of a game that get this right, let me know.
- Trigger effects via SDL.
The vanilla game has code referencing trigger effects, but I don't think Unity supports trigger effects on PC.
It should be possible to restore this functionality somehow.
- Better space walk HUD. 
I'm not not totally happy with it.
- More immersive gyro/flick stick controls in the stationary probe launcher.
The mouse/joystick sensitivity is reduced to add a feeling of weight.
Perhaps this should apply to flick stick and gyro too.
- Improve performance.
I made a passable attempt at not doing anything to totally tank the performance.
I haven't noticed a performance hit on my machine, but I haven't profiled either.
If the performance is causing problems for you, please file an issue.

## Licensing

This project is licensed under the [MIT](LICENSE) license.

This project contains [NaokoAF's GyroHelpers](https://github.com/NaokoAF/GyroHelpers/tree) as a submodule. GyroHelpers is licensed under the [MIT](https://github.com/NaokoAF/GyroHelpers/blob/master/LICENSE) license.

This project contains an [SDL3](https://wiki.libsdl.org/SDL3/FrontPage) binary. SDL3 is licensed under the [zlib](https://github.com/libsdl-org/SDL/blob/main/LICENSE.txt) license.

This project contains the [SDL3-CS](https://github.com/flibitijibibo/SDL3-CS) legacy bindings. SDL3-CS is licensed under the [zlib](https://github.com/flibitijibibo/SDL3-CS/blob/main/LICENSE) license.

## Special Thanks

- [NoakoAF](https://github.com/NaokoAF) for [GyroHelpers](https://github.com/NaokoAF/GyroHelpers) and for the wonderful [NeonGyro](https://github.com/NaokoAF/NeonGyro) mod.
NeonGyro is very fun, and I was able to save a lot of time and effort by consulting her work and using GyroHelpers.
- [Jibb Smart](https://www.karboom.net/) for [GyroWiki](http://gyrowiki.jibbsmart.com/) and for his other contributions to input design across the gaming industry.
Many games would be inaccessible or otherwise unenjoyable to me if not for flick stick.
I greatly appreciate the technical knowledge he has given to the community.
- [AmazingAlek](https://github.com/amazingalek), [Raicuparta](https://github.com/Raicuparta/), [_nebula](https://github.com/misternebula), [TAImatem](https://github.com/TAImatem), et al. for the incredible [Outer Wilds Mod Loader](https://github.com/ow-mods/owml).
My work would not be nearly as accessible or user friendly without OWML.
- [Sam Lantinga](https://github.com/slouken) and many many others for [SDL](https://github.com/libsdl-org/SDL).
- [flibitijibibo](https://github.com/flibitijibibo) et al. for [SDL3-CS](https://github.com/flibitijibibo/SDL3-CS).

