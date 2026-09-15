# OWMixedInput Settings Manual

This document describes the settings of OWMixedInput.
The settings default to what best approximates the vanilla behavior.
To make the most of this mod, you should change some settings to your liking.

## Table of Contents

* [Primary Control Scheme](#primary-control-scheme)
* [User Interface Settings](#user-interface-settings)
  * [User Interface Style](#user-interface-style)
  * [Mouse Cursor](#mouse-cursor)
  * [Virtual Keyboard](#virtual-keyboard)
* [Gamepad Settings](#gamepad-settings)
  * [Use Rumble](#use-rumble)
* [Mouse Settings](#mouse-settings)
  * [Mouse Smoothing](#mouse-smoothing)
  * [Mouse Sensitivity Scale](#mouse-sensitivity-scale)
* [Thruster Settings](#thruster-settings)
  * [Thruster Roll Axis](#thruster-roll-axis)
  * [Invert Roll](#invert-roll)
  * [Square XZ & Pitch/Yaw](#square-xz--pitchyaw)
  * [Extended Rotational Thruster Range](#extended-rotational-thruster-range)
* [Camera Settings](#camera-settings)
  * [Gamepad/Mouse Sensitivity Ratio](#gamepadmouse-sensitivity-ratio)
  * [Allow Free Look](#allow-free-look)
  * [Allow Slow Camera](#allow-slow-camera)
  * [Recenter Time](#recenter-time)
* [Gyro Settings](#gyro-settings)
  * [Gyro Camera](#gyro-camera)
  * [Sensitivity](#sensitivity)
  * [Sensitivity Ratio](#sensitivity-ratio)
  * [Space](#space)
  * [Gyro Button Behavior](#gyro-button-behavior)
  * [Calibrate Button Behavior](#calibrate-button-behavior)
  * [Calibrate On Connect](#calibrate-on-connect)
  * [Tightening Threshold](#tightening-threshold)
  * [Smoothing](#smoothing)
  * [Acceleration](#acceleration)
* [Flick Stick Settings](#flick-stick-settings)
  * [Flick Stick](#flick-stick)
  * [Flick Threshold](#flick-threshold)
  * [Flick Time](#flick-time)
  * [Snapping Zones](#snapping-zones)
  * [Forward Deadzone](#forward-deadzone)
  * [Angle Smoothing](#angle-smoothing)
* [Nitpicky Settings](#nitpicky-settings)
  * [Helmet Shake](#helmet-shake)
  * [Allow Input Fading](#allow-input-fading)
  * [Probe Overlap Checks](#probe-overlap-checks)
  * [Space Walk Camera Range](#space-walk-camera-range)
* [Control Scheme Overrides](#control-scheme-overrides)
  * [Probe Launcher Control Scheme](#probe-launcher-control-scheme)
  * [Autopilot Control Scheme](#autopilot-control-scheme)
  * [Ship Log Control Scheme](#ship-log-control-scheme)
  * [Roasting Stick Control Scheme](#roasting-stick-control-scheme)
* [Control Bindings](#control-bindings)
  * [Gyro](#gyro)
  * [Calibrate](#calibrate)
  * [Recenter Camera](#recenter-camera)

# Primary Control Scheme

Select the primary control scheme for this mod.

Options: 'Last Input', 'Gamepad', 'KB&M'

Default: 'Last Input'

Recommended: 'Gamepad' or 'KB&M'

The vanilla game uses various checks (e.g. `OWInput.UsingGamepad` and `InputUtil.IsMouseMoveAxis`) to change behavior based on what devices it thinks the player is using.
All such code has been modified to take both branches at all times or, if that is infeasible, to accept a player setting instead.

In the vanilla game, these checks are determined via the most recent "active control" according to Unity's input system.
The precise ordering of these checks is non-deterministic.

In this mod, you can set your control scheme to 'Last Input' which will approximate the vanilla behavior.
This setting still uses Unity's active controls, however the checks are made in a deterministic order once per-frame.
The reported control scheme is a deterministic function of the actuated input devices according to Unity and your specific control bindings.

If you are using multiple devices, it is still recommended to select your favored device, either gamepad or keyboard and mouse (KB&M).

On its own, this setting has no effect.
However, other settings which are set to 'Default' will match your primary control scheme instead of the vanilla 'Last Input' behavior.

# User Interface Settings

## User Interface Style

Select the style of button prompts and menus. 

Options: 'Default', 'Last Input', 'Gamepad', 'KB&M'

You can use this setting if you prefer the UI to look one way but your controls to match a different device. 
If your UI is flickering between the two styles, use 'Gamepad' or 'KB&M' or use 'Default' and set your primary control scheme to 'Gamepad' or 'KB&M'.

This setting also affects the UI control scheme in small ways.
The mod will refuse to display the gamepad UI until a gamepad input is detected so that you don't somehow get locked out of the UI.

## Mouse Cursor

Select whether the game can display the mouse cursor.

Options: 'Default', 'Last Input', 'On', 'Off'

You can use this setting if you want to use the mouse cursor with the gamepad UI or you do not want to use the mouse cursor with the KB&M UI.

## Virtual Keyboard

Choose when the game will display a virtual keyboard for text fields.

Options: 'Steam Deck', 'Big Picture Mode', 'Off'

Default: 'Steam Deck'

The game uses the Steam API to display a virtual keyboard. 
In vanilla, the game only attempts to display a keyboard when running on a Steam Deck.
You can set this setting to 'Big Picture Mode' to attempt to open a virtual keyboard whenever possible.
However, this API is only functional when Steam is running in Big Picture Mode.

# Gamepad Settings

## Use Rumble

Choose when the gamepad can rumble.
Note that the vanilla 'Rumble' setting must be enabled for rumble to function.

Options: 'Default', 'Last Input', 'Always', 'Never'

You can use this setting to enable rumble even when your control scheme is otherwise KB&M. 

# Mouse Settings

## Mouse Smoothing

The vanilla game includes mouse smoothing.
You can use this setting to turn it off.

Default: On

## Mouse Sensitivity Scale

Select the secret settings sensitivity scale for mouse inputs.

Options: 'Mouse', 'Joystick'

Default: 'Mouse'

This setting refers to the `secretsettings.txt` sensitivity scales.
For some reason, the game switches sensitivity values at runtime according to `UsingGamepad`, rather than using different settings per-device.

When set to 'Mouse', the mod will use the values `MinSensMouse` and `MaxSensMouse` for mouse sensitivity.
This is the vanilla behavior for mice.

When set to 'Gamepad', the mod will use the values `MinSensJoystick` and `MaxSensJoystick` for mouse sensitivity calculations.
The default values for the settings match the mouse defaults prior to patch 15.

Gamepad inputs will always use `MinSensJoystick` and `MaxSensJoystick`. 

# Thruster Settings

## Thruster Roll Axis

Select which inputs are used as roll in 'Roll Mode'. 

Options: 'Yaw', 'X', 'Up/down'

Default: 'Yaw'

The 'Yaw' option uses 'Look-Y'.
The 'X' option uses 'Move-X'. 
The 'Up/down' option uses 'Thrust Up' and 'Thrust Down'.

Use 'Yaw' for vanilla behavior.

## Invert Roll

Invert ship roll in 'Roll Mode'.

This inverts the direction of roll inputs, not the 'Roll Mode' input itself.

## Square XZ & Pitch/Yaw

This pair of settings allows you to pick the 'shape' of your thruster inputs.
The 'Square XZ' setting affects the translational X and Z thrusters, and the 'Square Pitch/Yaw' setting affects the rotational pitch and yaw thrusters.

Default: Off

In the vanilla game, gamepad and button thruster inputs (joystick, WASD) are clamped to the unit disk.
However, the ship has 6 independent pairs of opposing thrusters: three translational axes and three rotational axes. 
The result is that the X and Z thrusters cannot fire at maximum strength at the same time.
Similarly with the pitch and yaw thrusters.

If these settings are enabled, the corresponding thruster inputs are stretched to a square using supercircle interpolation (see [DETAILS.md](DETAILS.md) for details).
Thus, any pair of orthogonal thrusters can fire at maximum strength simultaneously. 
For example, holding the left stick at 45 degrees NW or pressing W and A simultaneously will fire both the forward and left thrusters at maximum strength simultaneously.
This may result in thruster controls feeling more responsive.

This setting does not affect mouse inputs, which always have access to the full range of the thrusters.

## Extended Rotational Thruster Range

Double the range of the allowed rotational thruster inputs.

Default: On

The mouse inputs for the rotational thrusters are kind of weird: the velocity of the mouse controls the rotational acceleration and is independently clamped in each axis.
In the vanilla game, the rotational thruster output can be up to twice the maximum thruster strength in the KB&M control scheme.
Presumably, this is to make the rotational thrusters feel more responsive due to less input clamping.

This setting does not affect joystick and button inputs which are still limited in magnitude.

The mouse thruster controls are slightly different between vanilla and this mod, but setting this to 'On' best approximates the vanilla behavior.

# Camera Settings

## Gamepad/Mouse Sensitivity Ratio

Camera vertical sensitivity ratio for gamepad/mouse inputs.

Gamepad Default: 0.75

Mouse Default: 1.0

0.75 means vertical sensitivity is 75% slower.
The vanilla game scales vertical gamepad inputs down by 75% but leaves mouse inputs alone.
Now, you can pick these values for yourself.

## Allow Free Look

Toggle the vanilla 'Free Look' feature. 

Default: On

You may wish to disable free look when using the gyro camera, as it serves a similar purpose.

## Allow Slow Camera

Allow the game to slow camera inputs when zooming/waking.

Default: On

You may find this helpful/immersive or you may find this annoying.
If you find the latter is the case, turn this setting off.

Input fading has a similar effect with different causes.
It can be controlled with the ['Allow Input Fading'](#allow-input-fading) setting.

## Recenter Time

The amount of time it takes to recenter the camera when the ['Recenter Camera'](#recenter-camera) command is activated.
Measured in milliseconds. 

Default: 200ms

The binding for the recenter button is at the bottom of the settings menu.

# Gyro Settings

## Gyro Camera

Use gamepad gyro to control the camera (and the roasting stick).

Default: Off

The gyro camera will allow you to move the camera when it would otherwise be impossible in the vanilla game.
For example, you can move the camera in the ship cockpit using gyro without using 'Free Look'.
You can also move the camera while on a space walk.
This can be disorienting, so you may use the ['Space Walk Camera Range'](#space-walk-camera-range) settings to limit or disable this ability.

The gyro camera pairs well with ['Flick Stick'](#flick-stick).
The ['Recenter Camera'](#recenter-camera) command may also be useful.

See [README.md](README.md) for the supported controllers.

## Sensitivity

Gryo sensitivity in natural units (i.e. 2.0 means the camera rotates twice as far as your controller).

Default: 1.0

Players using gyro as (one of) their primary inputs will usually prefer the gyro sensitivity to be higher, perhaps between 2 and 8.

## Sensitivity Ratio

Gyro vertical sensitivity ratio. 

Default: 1.0

0.75 means vertical sensitivity is 75% slower.

## Space

Algorithm used to convert gyro input to camera turns.

Options: 'Local Yaw', 'Local Roll', 'Player Turn', 'Player Lean'

Recommended: 'Player Turn' or 'Player Lean'.

'Local Space' inputs are considered relative to the controller itself and use only 2 of the 3 rotational axes.
This is probably what you want for a handheld device.

'Player Space' inputs use a fusion of all three rotational axes and a gravity vector estimated using the accelerometer. 
This is the recommended choice for controllers.

Try them out and see which one you like best!

See [GyroWiki](http://gyrowiki.jibbsmart.com/blog:player-space-gyro-and-alternatives-explained) for an in-depth explanation of these algorithms. 

'World Space' and 'Laser Pointer' are not currently supported.
If you really want either of these added, please file an issue.

## Gyro Button Behavior

Select the behavior of the ['Gyro'](#gyro) command.

Options: 'Disabled', 'Hold', 'Mute', or 'Toggle'

Default: 'Disabled'

When 'Hold' is selected, gyro inputs will only be processed with the gyro button is held.

When 'Mute' is selected, gyro inputs will be processed *unless* the gyro button is held.

When 'Toggle' is selected, gyro inputs will not be processed by default but can be toggled by pressing the gyro button.

When 'Disabled' is selected, gyro inputs will always be processed and the gyro button will have no functionality.

Choose your binding at the bottom of the settings menu.

## Calibrate Button Behavior

Select the behavior of the ['Calibrate'](#calibrate) command.

Options: 'In Menus', 'Press', 'Hold'

Default: 'In Menus'

When 'Press' is selected, gyro calibration will occur immediately once the button is pressed.

When 'Hold' is selected, gyro calibration will occur after the button is briefly held.

When 'In Menus' is selected, gyro calibration will occur when pressing the button while in a menu.

'Hold' or 'In Menus' is recommended if you select a binding which overlaps with another control.
Otherwise, you will end up accidentally recalibrating your controller while it is in motion, resulting in gyro drift.

'Press' is recommended if you select a binding that does not overlap with any other control.

## Calibrate On Connect

Calibrate gamepads immediately upon connection.

Default: On

If this setting is enabled, leave your controller on a fixed surface while the game is launching.
Otherwise, you will experience gyro drift.
If this is annoying you, you can disable this calibration, but remember to calibrate your controller manually with the ['Calibrate'](#calibrate) command!

## Tightening Threshold

Smoothly squeezes rotations below this threshold toward 0, like a soft deadzone.
Measured in degrees per second. 

Default: 6deg/s

Helps reduce effects of shaky hands.

In code, this looks like
```
if (input.magnitude < threshold) {
    input *= input.magnitude / threshold
}
```

## Smoothing

Gyro smoothing is implemented with a pair of settings: 'Smoothing Threshold' and 'Smoothing Time'.
The threshold is measured in degrees per second and the time is measured in milliseconds.

Defaults: 
- Threshold: 0deg/s
- Time: 75ms

Rotations slower than the threshold are smoothed.
Helps reduce effects of shaky hands, but adds latency.

Smoothing is implemented via a rolling-average such that the eventual net displacement of an input is the same as an un-smoothed input.
'Smoothing Time' is the duration of the rolling-average buffer.
Essentially, this is the maximum latency caused by smoothing.
Larger values result in more powerful smoothing and more latency.

We do not provide a second threshold to implement intermediate smoothing amounts.
If this is important to you, please file an issue.

## Acceleration

This mod supports gyro acceleration in the form of four settings: 'Acceleration Threshold Slow', 'Acceleration Threshold Fast', 'Acceleration Sensitivity Slow' and 'Acceleration Sensitivity Fast'.
The thresholds are measured in degrees per second, and the sensitivities are measured in natural units.
The default values result in no acceleration.

Defaults:
- Threshold Slow: 0deg/s
- Threshold Fast: 75deg/s
- Sensitivity Slow: 1.0
- Sensitivity Fast: 1.0

Depending on how you use it, gyro acceleration can be used to make small movements more precise or can be used to fast movements more impactful, or both.
Like mouse acceleration, gyro acceleration effectively results in a sensitivity which depends on rotation speed.

Rotations slower than the 'Threshold Slow' will use use the 'Sensitivity Slow' value.
Rotations faster than the 'Threshold Fast' will use the 'Sensitivity Fast' value.
Rotations in between the two thresholds will use a sensitivity interpolated between the two sensitivities. 

Acceleration is applied on top of the gyro ['Sensitivty'](#sensitivity) setting.

# Flick Stick Settings

## Flick Stick

Replace joystick camera with flick stick.

Default: Off

With flick stick, the angle of the look joystick is used to directly control character/camera's rotation.
Flick stick may be unintuitive at first but if you haven't tried it before, you should!
It's fun!

Flick stick only really makes sense with the gyro camera enabled.
Thrusters, including the jetpack, will control as usual.
Mouse inputs are unchanged.
Gyro camera does not need to be enabled for this setting to take effect.

See [GyroWiki](http://gyrowiki.jibbsmart.com/blog:good-gyro-controls-part-2:the-flick-stick) for a detailed explanation.
There are many other tutorials available online.

## Flick Threshold

Activation level required to trigger a flick. 
0.9 means the joystick needs to be pushed 90% of the way to maximum in some direction in order to initiate a flick.

Default: 0.9

Recommended: 0.9-0.99

The threshold is applied after joystick deadzoning but does not consider joystick sensitivity.

## Flick Time

Amount of time it takes to perform an initial flick. Measured in milliseconds.

Default: 100ms

## Snapping Zones

Number of snapping zones.

Default: None

When using two or more snapping zones, initial flicks are rounded to the center of the zone.
When using one snapping zone, the ['Forward Deadzone'](#forward-deadzone) setting is consulted to determine whether or not to snap.

## Forward Deadzone

When ['Snapping Zones'](#snapping-zones) is 'One', initial flicks within 'Forward Deadzone' degrees of forward in either direction are rounded to zero.

Default: 7deg

Helps avoid unintended flicks when moving the stick up.

## Angle Smoothing

Angle smoothing is implemented with a pair of settings: 'Smoothing Threshold' and 'Smoothing Time'.
The threshold is measured in degrees and the time is measured in milliseconds.
Angle changes smaller than the threshold are smoothed across this duration.

Defaults: 
- Threshold: 2.3deg
- Time: 64ms

Helps compensate for limited joystick resolution.
Leave at default if unsure.

# Nitpicky Settings

## Helmet Shake

Enable helmet HUD shake based on look inputs. 

Default: On

## Allow Input Fading

Allow the game to fade inputs on rare occasions (e.g. loop beginnings and ends).

Default: On

Input fading affects all inputs.

Slow camera has a similar effect with different causes.
It can be controlled with the ['Allow Slow Camera'](#allow-slow-camera) setting.

## Probe Overlap Checks

Select which 'Tool Action' commands are checked when determining if 'Retrieve Scout' command must be held to activate.

Options: 'Primary', 'Both'

Default: 'Primary'

If 'Primary' is selected, the secondary action will be disabled when it overlaps with 'Retrieve Scout'.
If 'Both' is selected, 'Retrieve Scout' must be held to activate if it overlaps with either 'Tool Action Primary' or 'Tool Action Secondary'.

## Space Walk Camera Range

The range of the camera axes during a space walk.
Measured in degrees.

Defaults:
- X-axis: 40deg
- Y-axis: 160deg

This is only relevant when using gyro controls as otherwise the camera is fixed during a space walk.
You may wish to leave this value relatively small or else you may forget which direction is forward.
This is particularly relevant in the X-direction.

# Control Scheme Overrides

These settings determine the few control aspects that I did not unify across control schemes.
When set to 'Default', they will match the ['Primary Control Scheme'](#primary-control-scheme).
When set to 'Last Input', they will match the most recently used device, like the vanilla game.
However, this can result in significant control jank.

## Probe Launcher Control Scheme

Select the control scheme for probe launchers.

Options: 'Default', 'Last Input', 'Hybrid', 'Gamepad', 'KB&M'

Whether the 'Retrieve Scout' command must be held to activate is determined by whether it overlaps with the 'Tool Action' commands in the selected control scheme.
Use 'Hybrid' to check overlaps in both control schemes.

## Autopilot Control Scheme

Select the control scheme for the ship's autopilot.

Options: 'Default', 'Last Input', 'Gamepad', 'KB&M'

When the control scheme is 'Gamepad', autopilot is disabled when a probe is anchored to avoid overlapping controls.

## Ship Log Control Scheme

Select the control scheme for scrolling the description of the ship log.

Options: 'Default', 'Last Input', 'Gamepad', 'KB&M'

When the control scheme is 'Gamepad', 'Look' inputs are used to scroll the log.
When the control scheme is 'KB&M', 'Tool' inputs are used to scroll the log.

## Roasting Stick Control Scheme

Select the sensitivity for the roasting stick.

Options: 'Default', 'Last Input', 'Gamepad', 'KB&M'

For some reason, the controls are more sensitive when the control scheme is 'Gamepad'.

# Control Bindings

All control bindings default to 'None'.
Once they are bound, I do not know of a way to reset them except to reset the entire mod settings.

The control bindings are not stored in `config.json`.
You can use the 'Open Folder' option in the Outer Wilds Mod Manager, backup your `config.json`, reset your settings to default, and then restore your `config.json` to unset your bindings without losing your settings.

## Gyro

Command used for enabling/disabling gyro based on the ['Gyro Button Behavior'](#gyro-button-behavior) setting. 

This binding has no effect unless ['Gyro Camera'](#gyro-camera) is enabled.

## Calibrate

Command used to calibrate the gyro.

Lay the controller on a flat surface and hold this button down for a second or two to correct drift.
Affected by the ['Calibrate Button Behavior'](#calibrate-button-behavior) setting.

This binding has no effect unless ['Gyro Camera'](#gyro-camera) is enabled.

## Recenter Camera

Command used to recenter the camera Y-axis when controlling the player.

The ['Recenter Time'](#recenter-time) setting can be used to set how long the camera reset takes.

This command does not require the gyro camera to function.
