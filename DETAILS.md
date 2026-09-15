# OWMixedInput Detailed Changes

This document describes the changes implemented by OWMixedInput in significant detail.
Players may wish to consult this document if they require a detailed list of behavior changes introduced by this mod.
Mod developers may wish to consult this document to learn how OWMixedInput will interact with their mods;
you may want to grep for the classes that are important to you to see if I touch them.
This document also serves as my personal reference notes: what behaviors did I change or not change and for what reasons?

## Table of Contents

* [Behavior Changes](#behavior-changes)
    * [Camera Controls](#camera-controls)
    * [Thruster Controls](#thruster-controls)
    * [Deadzones](#deadzones)
    * [Button Activation](#button-activation)
    * [Axis Mapping](#axis-mapping)
    * [Input Clamping](#input-clamping)
    * [Mouse Accumulation](#mouse-accumulation)
    * [Roll Prompts](#roll-prompts)
    * [Roasting Stick](#roasting-stick)
    * [Mouse Responsiveness](#mouse-responsiveness)
    * [Gyro and Flick Stick](#gyro-and-flick-stick)
    * [Other Behaviors Changed by Settings](#other-behaviors-changed-by-settings)
* [API Changes](#api-changes)
    * [SharedInputManager](#sharedinputmanager)
    * [UsingGamepad](#usinggamepad)
    * [LastCommandInputType](#lastcommandinputtype)
    * [OnUpdateUsingGamepadChange](#onupdateusinggamepadchange)
    * [OnUpdateInputDevice](#onupdateinputdevice)
    * [InputManager.Update](#inputmanagerupdate)
    * [_inputFadeFaction](#_inputfadefaction)
    * [MouseDelta](#mousedelta)
    * [UpdateLastInputCommandType](#updatelastinputcommandtype)
    * [IInputAction](#iinputaction)
    * [IInputCommands](#iinputcommands)
* [API Additions](#api-additions)
    * [GetValue and GetAxisValue](#getvalue-and-getaxisvalue)
    * [GyroManager](#gyromanager)
    * [MixedInputManager](#mixedinputmanager)
        * [UsingGamepadForPrimary](#usinggamepadforprimary)
        * [LastInputUsingGamepad](#lastinputusinggamepad)
        * [deltaTime, ProcessDirectInput](#deltatime-processdirectinput)
        * [OnUpdateInputSettings](#onupdateinputsettings)
        * [Event Raising Methods](#event-raising-methods)
        * [UpdateFromAction](#updatefromaction)
    * [OWMixedInput](#owmixedinput)
    * [Squareification](#squareification)
    * [Extension Methods](#extension-methods)
* [Patch/Override List](#patchoverride-list)
    * [OWMixedInput.cs](#owmixedinputcs)
    * [UIPatches.cs](#uipatchescs)
    * [CameraPatches.cs](#camerapatchescs)
    * [ThrusterPatches.cs](#thrusterpatchescs)
    * [InputPatches.cs](#inputpatchescs)
    * [InputActionPatches.cs](#inputactionpatchescs)
    * [InputCommandsPatches.cs](#inputcommandspatchescs)
* [Relevant Unpatched Code](#relevant-unpatched-code)

## Behavior Changes

Most of the behavior changes in this mod are caused by a setting.
See [`SETTINGS.md`](SETTINGS.md) for details.
All known behavior changes are documented below, including those which cannot be enabled/disabled with a setting.

### Camera Controls

The camera controls have been substantially rewritten.
This mod intends to accomplish the same goals as [Mouse Fix by Novaenia](https://outerwildsmods.com/mods/mousefix/), but we do not use all the same approaches.
Additionally, new logic has been put in place to support many player settings.

Camera updates are now processed each frame instead of once per physics tick.
The camera angle is transferred to the player model during a physics tick.

The camera improvements apply to the player camera as well as to the stationary probe launcher camera.

Settings for the camera sensitivity ratio for gamepad and mouse inputs are exposed separately.
Like vanilla, the sensitivity ratio does not apply to the free look or stationary probe launcher cameras.
If this annoys you, please file an issue.

The camera can be adjusted much closer to straight up or straight down instead of being locked to 80 degrees in either direction.

The camera is no longer slowed depending on the timescale.
I do not think this has a noticeable difference.

The 'Slow Camera' effect can be disabled with a setting.
The camera is slowed in a few situations, in particular when using the scope zoom.
If you want the same inputs to always result in the same angle changes, disable this setting.
You may want to disable with 'Input Fading' setting as well.

The value used to control the player model rotation animation is computed each frame and read each physics tick.
These values are smoothed.
The smoothing can result in stale values, but it really should not matter.
Still, the animations can be choppy animations when using the mouse, especially when mouse smoothing is disabled.
This is actually vanilla behavior.
I don't understand the animation code enough to fix this issue further, but it's a very minor issue; it's only noticeable when you look down at the player model while spinning.

Gyro camera and flick stick can be enabled with a setting.
See ['Gyro and Flick Stick'](#gyro-and-flick-stick) for details.
Flick stick replaces joystick look when it is enabled.

The 'Free Look' functionality of the vanilla game can be disabled.
When disabled, it is as if the 'Free Look' input binding has no uses.
The UI prompts are updated accordingly.
This is may be desirable when using the gyro camera.

A 'Recenter Camera' binding has been added to reset the camera Y-axis.
The amount of time this takes can be adjusted with a setting.
The camera is allowed to be reset whenever the camera may be control by other inputs.
This binding may be especially useful when using gyro and flick stick.

### Thruster Controls

The thruster controls have been substantially rewritten. 
This includes the jetpack controls.
New logic has been put in place to support many player settings.

The player's selected roll axis is used instead of always using yaw.
The roll button prompts are updated to match the selected roll axis.
The roll axis can also be independently inverted.

The range of rotational thruster strength now follows a setting instead of varying with active devices.
Gamepad inputs do not have access to this extra range as they are clamped.

Inputs may be squared with a player setting.
This allow greater control over the thrusters but allows faster movement than is possible in vanilla.

Mouse inputs have been reworked. 
They might feel different.
Specifically, mouse inputs have been reworked such that (assuming the same sensitivity settings), a joystick input and mouse input which have the same effect on the camera will also have the same effect on the thrusters (modulo getting clamped by the thruster range);
the thruster acceleration is controlled by the mouse velocity.
I consider this to be an improvement, but who knows.
If you think they feel worse, please file an issue.

The landing thruster controls have been adjusted slightly to fix what I think is a bug.
I doubt this will be noticeable.
The change is most extreme when locked onto a planet, flying near the surface, using the landing camera, and not oriented toward the planet.

Additionally, The landing thruster corrections are no longer normalized, which allows them to take full advantage of the thrusters.
Your orbit speed may decay slightly faster than vanilla when using the landing camera.

Gyro controls are not added to the thrusters.

### Deadzones

The deadzone code has been rewritten.
The behavior differences should be minor in most cases.

If one axis of a joystick is bound to a control, deadzoning is applied to the entire joystick then projected to the single axis as opposed to individually deadzoning the single axis.

Non-joystick inputs are no longer deadzoned. 
This includes analog triggers and possibly pressure sensitive buttons.
If this is a problem for you, please file an issue.

### Button Activation

All activation logic has been replaced with a simple `value > PressedThreshold` check.
This should not result in any differences except in a single scenario:
if a button command is mapped to a mouse axis (perhaps by manually editing your bindings), it can be "spammed" instead of having a small delay before it is considered released/inactive.

### Axis Mapping

In the vanilla input code, if you switched the axes of a composite binding (i.e. bound 'Look X' to 'Right Stick Y' and 'Look Y' to 'Right Stick X'), it would be sometimes be switched back when processing input values.
Now, you can bind an X-axis to a Y-axis or vice versa if you want to.
If you were relying on the vanilla behavior and your axes are now switched, change your vanilla input bindings to the ones you expect.

### Input Clamping

The input methods return values clamped to the unit disk unless special support for unclamped values has been added.

This fixes a bug where player movement on the ground and in the air could apply more velocity than was intended to be possible when using particularly crafted controller layouts.

### Mouse Accumulation

Mouse input is polled every frame (`Update`) but only processed every physics tick (`FixedUpdate`).
Input is accumulated in `Update` and cleared after a `FixedUpdate`.

Unlike vanilla, accumulated inputs are not cleared when time is paused.
I don't believe this will have any noticeable negative consequences.

This mod fixes a bug where mouse inputs are duplicated across physics ticks during a single frame.
This is noticeable if your GPU is underpowered relative to your CPU or if you set a large physics rate.

### Roll Prompts

When the roll axis is 'Yaw' (as in vanilla), the roll prompt shows the look X-axis icon instead of the two axis look icon that the vanilla game uses.

### Roasting Stick

The roasting stick extension method no longer allows leaving the stick partially extended with button inputs.
This was basically impossible anyway as you had to hold down the button for such a minuscule amount of time.

The roasting stick rotation sensitivity can be selected with a setting instead of varying with active devices.
Gyro controls have been added to the roasting stick.

### Mouse Responsiveness

The mouse input code has been adjusted in many places.
- Camera controls including stationary probe launchers
- Thruster controls including the jetpack
- Roasting stick rotation
- Map rotation

The new input code should feel more responsive or at least no worse than vanilla.
If mouse inputs are used for other controls (e.g. by manually editing the control bindings), mouse inputs may feel worse.

Mouse smoothing can be disabled with a setting.

The `secretsettings.txt` mouse sensitivity scale can be chosen with a setting.

### Gyro and Flick Stick

A gyro camera and flick stick can be enabled independently with a pair of settings.
They are highly configurable.
See [`SETTINGS.md`](SETTINGS.md) for details.
Flick stick controls are only enabled when the player camera can be controlled via look inputs (e.g. not during a space walk).

The vanilla game will freeze time in certain situations (e.g. reading) when no player inputs are active.
If the gyro camera is enabled, gyro controls are the only exception.
You will still be able to move the gyro camera without unfreezing time.

Using gyro controls, you can now move the camera relative to the player during a space walk.
This is not possible in the vanilla game.
The range of the space walk camera can be set with a pair of settings.

When the gyro camera are enabled, gyro inputs will shake the helmet HUD just like look inputs.

The vanilla game occasionally locks the camera, for example when resetting the camera orientation to enter the ship cockpit or when entering a force field.
The gyro camera is not locked during these times.

When the gyro camera is enabled, the roasting stick can be controlled with gyro.

### Other Behaviors Changed by Settings

#### User Interface

The user interface style can be selected with a setting instead of varying with active devices.

The cursor can be enabled with a setting even when the gamepad UI is enabled.

UI using the `PopupInputMenu` class now attempt to open a virtual keyboard according to a player setting, instead of checking if the game is running on a Steam Deck. 
To my knowledge, this only affects the profile name text field.

The helmet HUD shake applied by look inputs can be disabled with a player setting.

#### Rumble

Rumble can be enabled even when KB&M inputs are used.
Like vanilla, rumble affects only the most recently used controller.

#### Input Fading

Input fading can be disable with a setting.
Input fading occurs in rare instance, for example at the boundary of time loops.
Input fading affects all inputs, including gyro controls but excluding flick stick controls.

#### Control Schemes

The ship log controls can be selected with a setting instead of varying with active devices.
This determines which inputs are used to scroll the log.

The autopilot control scheme can be selected with a setting instead of varying with active devices.
This only affects whether or not autopilot is available when the probe is anchored as the probe camera controls overlap with the autopilot button in the default gamepad mappings.

The probe launcher control scheme can be selected with a setting instead of varying with active devices.
This affects when the reverse camera is available.
In vanilla, this is computed by comparing bindings in the active control scheme.

## API Changes

### SharedInputManager

The `OWInput.SharedInputManager`/`OWInput.inputManagerInstance` is replaced with a custom subclass [`MixedInputManager`](#mixedinputmanager).
The vanilla `InputManager` instance is still constructed in the static constructor of `OWInput`.
The replacement occurs in the `OWMixedInput.Awake`, before the vanilla `InputManager` is awoken.

The static `OWInput` methods will delegate to the new `MixedInputManager` instance.

### UsingGamepad

The vanilla `InputManager.UsingGamepad` is overridden in [`MixedInputManager`](#mixedinputmanager).
It now returns are boolean indicating if UI related methods should use the gamepad UI.
It will not return `true` unless a gamepad is available, but it otherwise may or may not be related to the input devices that the player is actually using.

The `OWInput.UsingGamepad` method delegates to the new `MixedInputManager` override.
All vanilla usages of `UsingGamepad` for non-UI purposes have been patched out.

The base `InputManager.UsingGamepad` method may or may not be called via a call to `MixedInputManager.UsingGamepad`.
Additionally, the `InputManager.UsingGamepad` method may be called without a corresponding `MixedInputManager.UsingGamepad` invocation.

Patches to the vanilla `InputManager.UsingGamepad` will likely not perform as expected.

See the new APIs [`UsingGamepadForPrimary`](#usinggamepadforprimary) and [`LastInputUsingGamepad`](#lastinputusinggamepad) for alternatives.

### LastCommandInputType

The vanilla `InputManager.LastCommandInputType` was set to a single type.

The `MixedInputManager.LastCommandInputType` now refers to the *set* of input types that were encountered during the last frame which had an active input.

### OnUpdateUsingGamepadChange

The vanilla `OnUpdateUsingGamepadChange` event was only used by `PlayerData` to update the sensitivity settings.
Since that is no longer necessary, this event is deprecated and will no longer be raised.

Consider subscribing to [`OnUpdateInputDevice`](#onupdateinputdevice) instead.
If you were using an event for non-UI purposes, consider caching the relevant values in your `Update` routine and running the expensive operations only once they have changed.
If you really need an event, please file an issue.

### OnUpdateInputDevice

The vanilla `OnUpdateInputDevice` event was fired when the input manager's `LastCommandInputType` changed.
However, this event was only used to refresh cached values of [`UsingGamepad`](#usinggamepad) for UI routines.
Since `UsingGamepad` has now been repurposed for the UI, the `OnUpdateInputDevice` event has also been repurposed for the UI.

`OnUpdateInputDevice` is now raised when the value of `UsingGamepad` changes.
It is raised at most one per frame.

### InputManager.Update

The `InputManager.Update` method is overridden in [`MixedInputManager`](#mixedinputmanager).
The base method will not be called.

Patch `MixedInputManager.Update` instead of `InputManager.Update` when possible.

### _inputFadeFaction

Input fading can be disabled with a player setting.
Instead of using `InputManager._inputFadeFraction`, use `MixedInputManager.InputFadeFraction`.

The `GetValue` and `GetAxisValue` routines have been patched to use the new property.

### MouseDelta

The vanilla `InputManager.MouseDelta` was set to the mouse delta of the most recent `Update`.

`MixedInputManager.MouseDelta` will not be set and is deprecated.
Instead, read mouse values directly from `IInputCommands`.

See the new [`GetValue` and `GetAxisValue`](#getvalue-and-getaxisvalue) APIs for alternatives.

### UpdateLastInputCommandType

The vanilla `InputManager.UpdateLastInputCommandType` method is used by `IInputAction` event handlers to update the `LastInputCommandType` property.

This method is overridden in [`MixedInputManager`](#mixedinputmanager), and it is an error to invoke it.

Instead, the `MixedInputManager.UpdateFromAction` method is called in the `IInputAction.DoUpdate` methods.
See [`UpdateFromAction`](#updatefromaction) for more details.

### IInputAction

Recall the `IInputAction` hierarchy.
```
interface IInputAction;
interface IInputAction<T>: IInputAction where T: struct;
interface IAxisInputAction: IInputAction<float>, IInputAction;
inteface ISingleAction;
interface IInputActionPair: IInputAction<float>, IInputAction, IAxisInputAction;
interface IVectorInputAction: IInputAction<Vector2>, IInputAction;
interface IRebindableInputAction: IInputAction;
abstract class AbstractInputAction: IInputAction;
abstract class AbstractInputAction<T>: AbstractInputAction;
class BasicInputAction: AbstractInputAction<Vector2>, IInputAction<Vector2>, IInputAction, IVectorInputAction, ISingleAction;
class AxisInputAction: AbstractInputAction<float>, IInputAction<float>, IInputAction, IAxisInputAction, ISingleAction;
class InputActionPair: AbstractInputAction<float>, IInputAction<float>, IInputAction, IAxisInputAction, IInputActionPair;
class RebindableInputAction: BasicInputAction, IInputAction, IRebindableInputAction
class RebindableInputActionPair: InputActionPair, IInputAction, IRebindableInputAction
class RebindableAxisInputAction: AxisInputAction, IInputAction, IRebindableInputAction
```
Much of the `IInputAction` APIs have been changed.
If you were directly using `IInputAction`s, it is recommended to find an alternative.

It is now considered an error to use any `IInputAction` types which do not derive from `BasicInputAction`, `AxisInputAction`, or `InpurActionPair`.
You probably should not insert any other classes into the `IInputAction` hierarchy.

#### Events

The `OnStarted`, `OnPerformed`, and `OnCancelled` events are deprecated and will not be raised.

#### Phase

The `Phase` property is unsupported and will throw an exception when used.
You should not use Unity phase or interaction systems.

#### Value, Smoothing Buffers

The meaning of the `Value` property has changed substantially:
it now refers exclusively to the value of gamepad/non-mouse inputs.
Mouse inputs are now stored exclusively in the internal smoothing buffer(s) (regardless of whether or not smoothing is enabled).

#### HasActiveMouseInput

The `HasActiveMouseInput` property is unsupported and will throw an exception when used.
Determining behavior based on the active device is contrary to the idea of mixed input.

#### _mouseAxisControl

The `_mouseAxisControl` field is deprecated and will never be set.

#### AxisID

The `AxisID` property now refers to the axis ID that should be used for UI purposes.
It does not necessarily depend on what controls the player is currently using.
In particular, it should not be used for comparing bindings.

### IInputCommands

Recall the `IInputCommands` hierarchy.
```
interface IInputCommands;
interface ISingleInputCommand: IInputCommands;
interface ICompositeInputCommands: IInputCommands;
abstract class AbstractCommands: IInputCommands;
abstract class AbstractInputCommands<T>: AbtractCommands, IInputCommands, ISingleInputCommand
    where T: class, IInputAction;
abstract class AbstractCompositeInputCommands<T>: AbstractCommands, IInputCommands, ICompositeInputCommands
    where T: class, IInputAction;
class InputCommands: AbstractInputCommands<IVectorInputAction>;
class InputAxisCommands: AbstractInputCommands<IAxisInputAction>;
class CompositeInputCommands: AbstractCompositeInputCommands<IInputActionPair>;
[Obsolete] class RebindableInputCommands: AbstractInputCommands<RebindableAxisInputAction>;
[Obsolete] class RebindableAxisInputCommands: AbstractInputCommands<RebindableInputActionPair>;
[Obsolete] class RebindableCompositeInputCommands: CompositeInputCommands;
```
Much of the `IInputCommands` APIs have been changed.
The obsolete classes were not touched by this mod.
They are not used by vanilla, and you should not use them either.

It is now considered an error to use any `IInputCommands` types which do not derive from `InputCommands`, `InputAxisCommands`, or `CompositeInputCommands`. 
You probably should not insert any other classes into the `IInputCommands` hierarchy.

#### Events

The `OnStarted`, `OnPerformed`, and `OnCancelled` events are deprecated and will not be raised.
The vanilla game did not use these events (except for a no-op??).

#### Sensitivity

The meaning of the `Sensitivity` property has changed slightly:
A `NaN` sensitivity will be considered unset/null and will act as `1f` for both gamepad and mouse inputs.
Otherwise, the value is interpreted as the *gamepad* sensitivity.
In this case, mouse sensitivity is computed via `MixedInputManager.MouseSensitivity`.
There is no way to independently set the gamepad and mouse sensitivity.

The vanilla game only sets `Sensitivity` for look, yaw, and pitch inputs.

#### GetValue, GetAxisValue

The `GetValue` and `GetAxisValue` methods have been patched to use the defaults of the new API.
See the new [`GetValue` and `GetAxisValue`](#getvalue-and-getaxisvalue) APIs for details.

In particular, mouse inputs are converted to velocities, and inputs are clamped to the unit disk.
Neither of these is the vanilla behavior.

#### AxisValue, AxisValueAccumulated

The `AxisValue` and `AxisValueAccumulated` properties have changed meaning substantially:
they now refer exclusively to the values of mouse inputs. 
Non-mouse inputs are cached in the underlying `IInputAction`s instead of in the `IInputCommands`.

If you are using `AxisValue` or `AxisValueAccumulated`, switch to one of the new APIs.
See [`GetValue` and `GetAxisValue`](#getvalue-and-getaxisvalue) APIs for details.

#### ShouldAccumulate

This property is now meaningless. 
Mouse inputs will always accumulate and other inputs will never accumulate.
Getting or setting this property will throw an exception.

#### GetActiveDevice

This method is now unsupported and will throw an exception.
Determining behavior based on the active device is contrary to the idea of mixed input.
If you were using this, consider using some of the new APIs instead.

#### AxisID

The `AxisID` property now refers to the axis ID that should be used for UI purposes.
It does not necessarily depend on what controls the player is currently using.
In particular, it should not be used for comparing bindings.

## API Additions

### GetValue and GetAxisValue

The extension methods
```
extension(IInputCommands command) {
    public Vector2 GetAxisValue(
        bool useSensitivity = true,
        bool useInversionFactor = true,
        bool includeMouse = true,
        bool includeGamepad = true,
        bool asVelocity = true,
        bool clamp = true
    );

    public float GetValue(
        bool useSensitivity = true,
        bool useInversionFactor = true,
        bool includeMouse = true,
        bool includeGamepad = true,
        bool asVelocity = true,
        bool clamp = true
    );
}
```
are the recommended way to get input values from `IInputCommands`.

The `useSensitivityFactor` flag indicates that the `Sensitivity` value from the command should be used.
If it is `false`, `1f` will be used for all inputs.

The `useInversionFactor` flag indicates that the inversion factor(s) from the command should be used.
You should almost always set this flag to `true`, unless you only care about the magnitude of the result.

The `includeMouse` and `includeGamepad` flags indicate whether mouse and gamepad inputs should be included in the return value.
Gamepad inputs are considered to be everything except mouse axes, including mouse axes and keyboard buttons.

The `asVelocity` flag indicates whether the result should be considered as a velocity.
If `asVelocity` is `true`, gamepad inputs will be used directly and mouse inputs will be unscaled by an appropriate time delta.
If `asVelocity` is `false`, gamepad inputs will be pre-multiplied by `Time.unscaledDeltaTime` and mouse inputs will be used directly.

The `clamp` flag indicates whether the result will be clamped to the unit disk (i.e. should the result have magnitude at most 1).
If `GetValue` is used to get a single axis of a vector command, the clamping will be applied to the vector value and then projected onto the relevant axis.
This flag defaults to `true`, which is *not* the vanilla behavior.

These methods are also available as an instance method of `MixedInputManager` and as a static method of `OWMixedInput`.
These have an additional `InputMode mask = InputMode.All` parameter that can be used to mask inputs outside of certain input modes, like the vanilla methods by the same name.

### GyroManager

The `GyroManager` class exposes the API for gyro and for flick stick (even though flick stick does not require gyro to be enabled).
Internally, the `GyroManager` uses [SDL3](https://wiki.libsdl.org/SDL3/FrontPage) to read gyro values.

You can get a gyro manager from the `GyroManager` property on [`MixedInputManager`](#mixedinputmanager) or the static `GyroManager` property on `OWMixedInput`.

`GyroManager` exposes two boolean properties, `GyroEnabled` and `FlickEnabled` which indicate if the player has enabled the gyro camera and flick stick respectively.

The following methods can be used to obtain input values from the gyro and flick stick according to the player's settings.
```
public static Vector2 GetGyroValue(
    InputMode mask = InputMode.All,
    bool useSensitivity = true,
    bool asVelocity = false,
    bool clamp = false
);

public static float GetFlickValue(
    InputMode mask = InputMode.All,
    bool asVelocity = false,
    bool clamp = false
);
```
The parameters have the same meaning as those in [`GetValue` and `GetAxisValue`](#getvalue-and-getaxisvalue).

The return values are measured in radians.
`GetFlickValue` and the Y-axis of `GetGyroValue` refer to *negative* X-axis angles.
The X-axis of `GetGyroValue` refers to a positive Y-axis angle.
This is the convention used by [GyroHelpers](https://github.com/NaokoAF/GyroHelpers).

The value methods and boolean properties are also available on `MixedInputManager` instances and statically on the `OWMixedInput` class.

### MixedInputManager

`MixedInputManager` is a new subclass of `InputManager`.
The [`OWInput.SharedInputManager`](#sharedinputmanager) now refers to an instance of this subclass.

#### UsingGamepadForPrimary

The `UsingGamepadForPrimary` method returns a booleaning indicating if the player is using the gamepad as their primary control scheme.
If you need to different behavior depending on active devices, consider using this method instead of the `UsingGamepad` method, which has been repurposed for UI.

There is not an event to subscribe to changes to this value.
This method is also available as a static method on `OWMixedInput`.

#### LastInputUsingGamepad

The `LastInputUsingGamepad` method returns a boolean indicating if a gamepad was used during the last frame with active inputs.
This is a replacement for the vanilla `UsingGamepad` method, which has been repurposed for UI.

There is not an event to subscribe to changes to this value.
This method is also available as a static method on `OWMixedInput`.

#### deltaTime, ProcessDirectInput

Direct inputs (e.g. mouse axes, gyro, flick stick) are polled during `Update` but may be processed during `FixedUpdate`.
To accomplish this, inputs are accumulated during input routines and then cleared after a `FixedUpdate`.

If you are doing your own input accumulation, you may wish to use the `deltaTime` property or the `ProcessDirectInput` methods.

The `MixedInputManager.ProcessDirectInput` methods take a value from the most recent `Update` (the `value` argument), a value accumulated across `Update`s which is reset on the first `Update` after a `FixedUpdate` (the `accumulatedValue` argument), and a boolean `asVelocity`.

When `asVelocity` is `false`, `ProcessDirectInput` returns `value` during `Update` and returns `accumulatedValue` or zero during a `FixedUpdate`.
During the first `FixedUpdate` after an `Update`, `accumulatedValue` is returned, but during other `FixedUpdate`s, zero is returned.
This prevents reuse of stale inputs in consecutive `FixedUpdate`s.

When `asVelocity` is `true`, `ProcessDirectInput` returns a velocity by dividing by a time delta.
During an `Update`, `value / Time.unscaledDeltaTime` is returned.
During a `FixedUpdate`, the `accumulatedValue` is divided by a `deltaTime` value representing the accumulated `unscaledDeltaTime` across the `Update`s preceding it.
The same value is returned during consecutive `FixedUpdate`s with no `Update`s in between.
This reduces the error due to `Update` and `FixedUpdate` time deltas getting out of sync.

You should set `asVelocity` to `false` when you intend to use the result like a mouse input, and you should set `asVelocity` to `true` when you intend to use the result like a joystick/button input.

The `Time.unscaledDeltaTime` measured in a `FixedUpdate` is not the sum of the `Time.unscaledDeltaTime` values measured during the `Update` methods preceding it.
This can be problematic for converting direct inputs into velocities.
Use `ProcessDirectInput` if possible, but if you need to compute velocities yourself, consider using `MixedInputManager.deltaTime`.

`MixedInputManager.deltaTime` returns `Time.unscaledDeltaTime` during `Update` but returns an accumulated `unscaledDeltaTime` value during a `FixedUpdate`.
This is the value you should use to convert direct inputs into velocities.
Note that two `FixedUpdate`s can occur with no `Update`s in between.
Use `ProcessDirectInput(Time.unscaledDeltaTime, OWMixedInput.deltaTime, asVelocity: false)` if you do not want stale `deltaTime` accumulation.

These methods are also available as static methods on `OWMixedInput`.

#### OnUpdateInputSettings

A new event `OnUpdateInputSettings` has been added which is raised whenever the `OWMixedInput` settings are changed.

Currently, this is used by `ShipPromptController` and `JetpackPromptController` to refresh the roll prompt.

#### Event Raising Methods

The methods `RaiseUpdateInputSettings`, `RaiseUpdateInputMode`, `RaiseUpdateInputDevice` and `RaiseUsingGamepadChange` can be used to raise the `OnUpdateInputSettings`, `OnUpdateInputMode`, `OnUpdateInputDevice`, and `OnUpdateUsingGamepadChange` events respectively.

#### UpdateFromAction

This method is a replacement for the vanilla [`UpdateLastInputCommandType`](#updatelastinputcommandtype) method.
This method is used internally by OWMixedInput.
You probably should not call it yourself.

### OWMixedInput

Use `OWMixedInput.InputManager` to access `OWInput.SharedInputManager` as the `MixedInputManager` subclass.
`OWMixedInput` has the same relationship to its `InputManager` as `OWInput` has to `SharedInputManager`:
the static methods on `OWMixedInput` are convenience methods which delegate to the instance methods on `MixedInputManager`.

### Squareification

Input reshaping for the square thruster settings is accomplished via `MixedInputUtils.Squareify`.
This method accepts a `Vector2` on the unit disk and returns an output on the `[-1,1]^2` square.
The intention is that the mapping is "nice".
The method can also accept a `ref Vector2` to squareify a vector in place.

I have not seen this squareification algorithm elsewhere on the internet, but I would be surprised if no one has thought of it before.
The method I decided on could rightly be called supercircle interpolation or Lamé interpolation;
inputs lying on a circle of some radius are mapped to p-norm supercircles of the same radius.
See [Superellipse](https://en.wikipedia.org/wiki/Superellipse) for details.

Mathematically, the mapping is
```
v |-> r / |v|_p(r) * v                                                                              
    where r=|v| is the 2-norm/magnitude,                                                            
        |v|_p is the p-norm (|x|^p+|y|^p)^(1/p) for 1 <= p < \infty                                 
        |v|_\infty = max(|x|,|y|)                                                                  
        p(r) = 2/(1-r) if 0 <= r < 1 and p(r)=\infty otherwise. 
```
As a mapping from the disk to the square, this has many nice properties:
- The "radial angle" i.e. `atan2(y,x)` is preserved; 
the angle of player inputs is not modified.
- The mapping is asymptotically identity for small inputs;
small inputs are not modified much.
- The mapping is asymptotically identity near the axes;
"pure" X or Y inputs are not modified much.
- It is smooth everywhere except at the four points corresponding to the corners of the square;
the result shouldn't feel "jumpy".
- It is bijective;
any output on the square is accessible.

Of the methods I have seen, this algorithm is most similar to Fong's [FG-Squircular Mapping](https://squircular.blogspot.com/2015/09/fg-squircle-mapping.html).
The main advantage that this mapping has over the FG-Squircular mapping is that it has significantly less area distortion around the corners.
With Fong's map, the player needs to get their input extremely close to the boundary before their outputs are noticeably squared.

The deficiency in Fong's approach can be reduced by "linearizing" the squircle squareness.
Precisely, the squareness of an input circle of radius `r` is chosen such that the interpolation factor is linear in the radius on the `y=+-x` rays.
You can also linearize the supercircle interpolation, which amounts to setting `p(r) = 2/(1-2log2(1+(sqrt2-1)r))`.

This difference between the linearized supercircle interpolation and the one that I settled on is rather minor while the difference between Fong's formula and the linearized version is much more extreme.
You can play around with different versions of the mapping on [Desmos](https://www.desmos.com/calculator/jmiq2qnugp).

Note that neither my mapping not the linearized FG-Squircular mapping have a closed-form inverse in terms of standard operations.

The precise algorithm I settled on in `MixedInputUtils.cs` has the following properties:
- It is competitively fast.
I ran synthetic benchmarks against my best implementation of the linearized FG-Squircular mapping.
They were at most 2-3x apart on average, with the supercircle algorithm winning when inputs are biased toward magnitude 1.
Regardless, the runtime is measured in nanoseconds;
I doubt you could measure a difference in your game.
- It is numerically stable.
Branching is used to avoid precision issues as `p -> \infty` near the boundary.
The mathematical formula for the FG-Squircular mapping might suggest that branching could be avoided, but I had a hard time with precision issues near the boundary *and* near the origin with linearized version.
- It is simple.
There are no weird constants or long formulas.
I could rewrite my algorithm from memory, but I doubt I will ever memorize with linearized squareness formula.

But, maybe you can do better than me! 
If you find an algorithm you like better than mine, please let me know.

See also [Squircle](https://en.wikipedia.org/wiki/Squircle), [Analytical Methods for Squaring the Disc](https://arxiv.org/abs/1509.06344), and [Square/Disc mappings ](https://marc-b-reynolds.github.io/math/2017/01/08/SquareDisc.html).

### Extension Methods

The `AxisSmoothingBuffer.TryGetAverage` extension method can be used to extract values out of smoothing buffers.
If the player has disabled mouse smoothing, the raw input values are returned instead of the rolling average.

The `[IInputAction, IInputCommands].GamepadAxisID` extension methods can be used to extract the gamepad axis ID from an `IInputAction` or `IInputCommands` regardless of what control scheme is used for the UI.
Currently, this is used for rumble functionality, and `CompositeInputCommands` are not supported.

The `[IAxisInputAction, IVectorInputAction].MouseValue` extension methods can be used to extract mouse inputs from an `IInputAction`.
This is useful because the `Value` property only stored non-mouse inputs.
It is not recommended to use this method directly.
See ['Value, Smoothing Buffers'](#value-smoothing-buffers) for details.

The `IInputCommands.[RawGamepad, RawMouse]` extension properties can be used to read raw inputs from `IInputCommands`.
This is useful because the the `AxisValue` and `AxisValueAccumulated` properties have changed meaning.
See ['AxisValue, AxisValueAccumulated'](#axisvalue-axisvalueaccumulated).
It is recommended to use the new [`GetValue` and `GetAxisValue`](#getvalue-and-getaxisvalue) API instead.

## Patch/Override List

### OWMixedInput.cs

`OWInput.Awake` is prefixed to ensure that `OWMixedInput.Awake` is called before `OWInput.Awake`. 
This was added to guard against a bug that was previously in the code base.
It is safe to apply other patches to this method.

`InputManager.[GetAxisValue, GetValue]` are patched to use the new `InputFadeFraction` property.
Other patches to these methods are likely to cause unexpected behavior.

`InputManager.UsingGamepad` is overridden in `MixedInputManager`.
It now returns whether or not the gamepad UI should be used.
The base `InputManager` method may or may not be invoked by `MixedInputManager.UsingGamepad`.

`InputManager.Update` is overridden in `MixedInputManager`.
The base method will not be invoked.
Consider patching `MixedInputManager.Update` instead of `InputManager.Update`.

`InputManager.FixedUpdate` is overridden in `MixedInputManager`.
The override supports the newly available APIs.
The base method will still be invoked during a call to `MixedInputManager.FixedUpdate`.

`InputManager.[Awake, UsingGamepad, Update, FixedUpdate]` are prefixed to ensure that these methods are not called on non-`MixedInputManager` instances.
It is safe to apply other patches to these methods.
However, `Update` and `UsingGamepad` are overridden and may not be called.

`InputManager.UpdateLastInputCommandType` is overridden in `MixedInputManager`, and it is an error to invoke it.
The `MixedInputManager.UpdateFromAction` should be used instead.

### UIPatches.cs

`CursorManager.RefreshCursorState` is patched to follow a player setting instead of using `OWInput.UsingGamepad`.
It is safe to apply other patches to this method.

`PopupInputMenu.TryOpenVirtualKeyboard` is patched to follow a player setting, instead of checking if the game is running on a Steam Deck.
It is safe to apply other patches to this method.

`ShipPromptController.[Awake, OnDestroy]` and `JetpackPromptController.[Awake, OnDestroy]` are pre/postfixed to support reflecting the player selected roll axis in the roll UI prompt.
It is safe to apply other patches to these methods.

### CameraPatches.cs

This file adds substantial patches to the camera controls.
Adding additional patches to these methods will likely cause unexpected behavior.

`PlayerCameraController.UpdateInput` is replaced with a prefix patch.
This supports gyro, flick stick, and the new camera improvements.

`PlayerCameraController.UpdateRotation` is replaced with a prefix patch to enhance the camera clamping behavior.
This extends the normal range and makes sure the camera is clamped in the cockpit and during space walks.

`PlayerCameraController.SnapToDegreesOverSeconds` is postfixed to support the gyro camera during snapping.
The `_initSnapTime` field is repurposed as a 'snap progress' variable.

`PlayerCameraController.UpdateCamera` is replaced with a prefix patch to support the new snapping behavior.

`PlayerCharacterController.UpdateTurning` is replaced with a prefix patch to take its inputs from the camera controller instead of computing its own angled based on the look inputs.

`PlayerCameraController.Update` is replaced with a prefix patch to update the camera during `Update` instead of `FixedUpdate` and to support the 'Recenter Camera' command.

`PlayerCameraController.FixedUpdate` is disabled since the camera is updated during `Update.

`StationaryProbeLauncher.UpdateRotation` is replaced with a prefix patch to support gyro, flick stick, and the enhanced camera controls.

`HUDHelmetAnimator.Update` has been patched to disable helmet shake according to player settings and to add support for gyro controls.
It is safe to apply other patches to this method.

`[ShipCockpitController, ShipPromptController].Update` are patched to disable free look functionality according to the player setting.
It is safe to apply other patches to this method although this is a somewhat finicky transpiler patch. 

`ShipPromptController.Update` is patched to disable the free look prompt according to the player setting.
It is safe to apply other patches to this method although this is a somewhat finicky transpiler patch.
This patch is located here (instead of `UIPatches.cs`) to keep free look functionality confined to a single place.

`Locator.GetAlarmSequenceController` is postfixed to return `null` when it would return something `== null`.
This is kind of stupid, but it makes my life easier.
It is safe to apply other patches to this method.

### ThrusterPatches.cs

This file adds substantial patches to the thruster controls.
Adding additional patches to these methods will likely cause unexpected behavior.

`ThrusterModel.FireRotationalThrusters` is patched to set the thruster range using a player setting instead of `UsingGamepad`.

`ModelShipController.[ReadRotationalInput, ReadTranslationalInput]`, `JetpackThrusterController.[ReadRotationalInput, GetRawInput]`, `ShipThrusterController.[ReadRotationalInput, ReadTranslationalInput]` are replaced with prefix patches to support the new thruster controls.
Specifically, we add support for the roll axis setting and the square inputs settings.
The landing camera controls have also been slightly adjusted in `ShipThrusterController.ReadTranslationalInput`.

### InputPatches.cs

`[OWAxisProcessor, OWDoubleAxisProcessor].Process]` are patched to disable processing.
I considered removing the processors from the input bindings entirely, but as the binding data is serialized and persisted to disk, this impacted input processing after OWMixedInput was uninstalled.
Other patches to this method are likely to cause unexpected behavior.

`SettingsSave.UpdateSensitivySettings` is patched to always use gamepad sensitivity values.
We have new logic for apply sensitivity to mouse values.
See `MixedInputUtils.MouseSensitivity` and the [`IInputCommands`](#iinputcommands) API changes for more details.
It is safe to apply other patches to this method.

`RoastingStickController.UpdateExtension` has been patched to avoid calling `UsingGamepad`.
The gamepad logic is now always used.
It is safe to apply other patches to this method.

`RoastingStickController.UpdateRotation` is patched to use a player setting instead of `UsingGamepad`.
Direct inputs are used instead of velocities, and gyro control is added.
It is not recommended to apply other patches to this method.

`MapController.LateUpdate` is patched to use unclamped rotation values.
It is safe to apply other patches to this method.

`ShipLogEntryDescriptionField.[Start, Update]` are patched to use a player setting instead of `UsingGamepad`.
It is safe to apply other patches to this method.

`ShipCockpitController.IsAutopilotAvailable` is patched to use a player setting instead of `UsingGamepad`.
It is safe to apply other patches to this method.

`ProbeLauncher.[UpdatePostLaunch, IsReverseCamAvailable]` and `ProbePromptController.[Awake, Update]` are patched to follow a player setting for detecting overlapping bindings instead of using `IInputCommands.HasSameBinding`.
It is safe to apply other patches to these methods.

`RumbleManager.Update` is patched to refer to a player setting instead of `UsingGamepad`.
It is safe to apply other patches to this method.

`RumbleManager.TriggerEffect.[AffectsLeftTrigger, AffectsRightTrigger]` are patched to refer to gamepad axis IDs instead of `UsingGamepad`-based axis IDs.
It is safe to apply other patches to this method.
I don't think trigger effects are currently working on PC.
This is something to consider for later.

### InputActionPatches.cs

This file patches methods in the `IInputAction` hierarchy.
Most of the important logic has been rewritten.
Other patches applied to these classes likely will not behave properly.
See the [`IInputAction`](#iinputaction) API changes for more details.

`AbstractInputAction.EnableAction` is patched to avoid registering the event handlers.

`AbstractInputAction.Phase` is patched to throw an exception.
It is considered an error to use this property.

`[AbstractInputAction, InputActionPair].PhaseUpdate` is patched to throw an exception.
It is considered an error to call this method.

`AbstractInputAction.[InputStarted, InputPerformed, InputCancelled]` are patched to throw an exception.
It is considered an error to call these methods.

`AbstractInputAction.TryApplyMouseProcessing` is patched to throw an exception.
We use our own mouse processing logic.
It is considered an error to call this method.

`AbstractInputAction.HasActiveMouseInput` is patched to throw an exception.
It is considered an error to use this property.

`[BasicInputAction, AxisInputAction, InputActionPair].[SetAction, DoUpdate]` are patched to support the new API changes.

### InputCommandsPatches.cs

This file patches methods in the `IInputCommands` hierarchy.
Most of the important logic has been rewritten.
Other patches applied to these classes likely will not behave properly.
See the [`IInputCommands`](#iinputcommands) API changes for more details.

The `[InputCommands, InputAxisCommands, CompositeInputCommands]` constructors have been postfixed to initialize the `Sensitivity` field to `float.NaN`.

`AbstractCommands.Update` is patched to accumulate inputs even when time is paused.

`[InputCommands, InputAxisCommands, CompositeInputCommands].UpdateFromAction` are patched to support the API changed and to remove special-casing and complex behavior.
Deadzoning, clamping, axis mapping, and other necessary transformations occur elsewhere.
`AxisValue` is set to the underlying actions `MouseValue`.
The `IsActiveThisFrame` value is replaced with a simple comparison to `PressedThreshold`.

`AbstractInputCommands.UpdateFromAction` is patched to throw an exception.
It is considered an error to call `UpdateFromAction` in this way.
The patch applies to `AbstractInputCommands<T>` where `T in [IAxisInputAction, RebindableAxisInputAction, IVectorInputAction, RebindableInputActionPair]`.

`CompositeInputCommands.InitializeAxisID` is patched to avoid getting `ShouldAccumulate`.

`AbstractCommands.[GetAxisValue, GetValue]` are patched to delegate to the new extension methods.
See the new [`GetValue` and `GetAxisValue`](#getvalue-and-getaxisvalue) APIs for details.

`[AbstractInputCommands, AbstractCompositeInputCommands].GetUITextures` is patched to avoid calling `UsingGamepad`.
I am not sure why it ever did to begin with.
The patch only applied to `AbstractInputCommands<T>` where `T in [IAxisInputAction, RebindableAxisInputAction, IVectorInputAction, RebindableInputActionPair]` and `AbstractCompositeInputCommands<IInputActionPair>`.
Using any other generic variant is considered an error.

`AbstractCommands.ShouldAccumulate` is patched to throw an exception.
It is considered an error to call this method.

`[InputCommands, InputAxisCommands, CompositeInputCommands].GetActiveDevice` are patched to throw an exception.
It is considered an error to call these methods.

## Relevant Unpatched Code

`OWInputInteraction` has not been touched, even though the vanilla behavior is not particularly useful for this mod.
The Unity interaction system is only used to determine which input devices are active.
I considered removing the `OWInputInteraction` from the input bindings entirely, but as the binding data is serialized and persisted to disk, this impacted input processing after OWMixedInput was uninstalled.

`PlayerData` subscribes to `OnUpdateUsingGamepadChange`.
The event handler is a no-op now, and `OnUpdateUsingGamepadChange` is deprecated, so we don't need to do anything about this.

`ProfileMenuManager`, `TabbedMenu`, `MapController`, and `SettingsMenuView` subscribe to `OnUpdateInputDevice` for refreshing UI.
We don't need to do anything about this, as `OnUpdateInputDevice` has been repurposed for the UI.

The following classes use `UsingGamepad` for UI purposes such that patches were not needed:
- `ScreenPrompt`
- `PromptManager`
- `ButtonWithHotkeyImageElement`
- `DialogueBoxVer2`
- `TabbedMenu`

The following classes use `UsingGamepad` for UI purposes in ways that affect the control scheme.
If there is demand, we may need to patch these classes in the future:
- `ProfileMenuManager`
- `PopupMenu`
- `PopupTwoOptionMenu`
- `MenuStackManager`
- `OWMenuInputModule`

Note that `UsingGamepad` usages in `OWML` are not recorded in either of the above lists.

The following classes call `GetValue` or `GetAxisValue` in ways that did not require patching.
- `ShipLogDetectiveMode`
- `ThrustAndAttitudeIndicator`
- `Signalscope`
- `PlayerCharacterController`. This class benefits from input values being clamped by default.
- `NomaiTranslatorProp`
- `MindSlideProjector`

Both `NomaiTranslatorProp` and `MindSlideProjector` use look inputs but have not been patched to add gyro controls.
This means that time can remain frozen when the gyro is moving.

The following classes are input related but did not require specific patching.
These classes may get patches in the future to support new features:
- `IInputManager`
- `UsingGamepadChangeEvent`
- `ReadInputManager`.
In the future, we could make this readout more useful.
- `InputUtil`. Note that the `IsJoystickAxis` and `IsMouseMoveAxis` are problematic and should probably not be used.
- `InputActionUtil`
- `InputCommandManager`. It is important to leave this class compatible with the input rebinding code from OWML.
- `InputTranslator`
- `InputTransitionUtil`
- `OWInputProcessorUtil`. We still use the deadzone logic from this class.
- `DoubleAxis`, `MouseAxis`, `XboxAxis`. These classes seem to be entirely unused? Maybe they are used dynamically in ways I do not understand.
- `KeyRebindingDisplayElement`
- `SettingsMenuModel`
- `RebindingState`
- `GamePadConfig`
- `InputRebindableData`
- `SteamManager`
- `UITextType`
- `TitleCodeInputManager`. Does anyone know what the easter egg does? I couldn't get it to trigger.
- `IRebindableInputData`
- `InputRebindable`, `InputBinding`. These are obsolete.
- `TitleScreenAnimation`
- `TitleScreenManager`
- `InputConsts`
- `ControlPaths`
