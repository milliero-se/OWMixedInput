using HarmonyLib;
using System.Reflection;

using OWML.Common;
using OWML.ModHelper;

using SDL3;
using static SDL3.SDL;

using Steamworks;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OWMixedInput;

[HarmonyPatch]
public class OWMixedInput: ModBehaviour {
	public static OWMixedInput Instance;

    // OWInput.SharedInputManager has type IInputManager.
    // In order to call our MixedInputManager methods, you can access OWMixedInput.InputManager instead.
    // Both calls return the same object.
    public static MixedInputManager InputManager 
        => OWInput.inputManagerInstance switch {
            MixedInputManager inputManager => inputManager,
            _ => throw new Exception("MixedInputManager.InputManager used before OWInput.inputManagerInstance was replaced")
        };

    // OWInput.Awake must be called before OWMixedInput.Started.
    // Keep track of that state here.
    bool OWInputAwoken = false;

	public void Awake()
	{
		Instance = this;

        // Replace the OWInput.inputManagerInstance with a MixedInputManager as soon as possible.
        // Before, we did so in a prefix of OWInput.Awake, but that was not soon enough.
        // This seems to work.
        OWInput.inputManagerInstance = new MixedInputManager();

        // Use Harmony to patch assembly as soon as possible. Start is too late.
        new Harmony("MillieRose.OWMixedInput").PatchAll(Assembly.GetExecutingAssembly());
	}

    // OWMixedInput.Awake must run before OWInput.Awake.
    // Ensure that occured here.
    [HarmonyPrefix, HarmonyPatch(typeof(OWInput), nameof(OWInput.Awake))]
    static void OWInput_Awake_Prefix() {
        if (OWInput.inputManagerInstance is not MixedInputManager) {
            throw new Exception("Awake called on OWInput before inputManagerInstance was replaced");
        }
        OWMixedInput.Instance.OWInputAwoken = true;
    }

    // Messages cannot be logged before the ModBehavior has started, but we need to debug
    // early initialization code. To accomplish this, store a message backlog.
    // If this list is not null, WriteLine calls will append here instead of writing
    // to the console.
    List<(string, MessageType)> message_backlog = new List<(string, MessageType)>();

	public void Start()
	{
        // Clear message backlog
        var backlog = message_backlog;
        message_backlog = null;
        foreach (var (message, type) in backlog) {
            WriteLine(message, type);
        }

        // Check that OWInput.Awake was called first.
        if (!OWInputAwoken) {
            throw new Exception("OWInput was not successfully awoken before OWMixedInput started");
        }
        WriteLine("MixedInputManager successfully injected!", MessageType.Success);

        // Start the input manager
        InputManager.Start(ModHelper);
	}

    public void OnApplicationQuit() {
        // Delegate to InputManager to cleanup SDL
        InputManager.OnApplicationQuit();
    }

    // Write a line to the console, or to the backlog if it has not been nulled.
    public void WriteLine(string message, MessageType type = MessageType.Message) {
        if (message_backlog is not null) {
            message_backlog.Add((message, type));
            return;
        }

        ModHelper.Console.WriteLine(message, type);
    }
    // static WriteLine method.
    //
    // If this has an nullable parameter, the static method can have the same name as the
    // instance method because it's technically not the same type. Lol.
    public static void WriteLine(string message, MessageType? type = null) {
        if (type is MessageType t) {
            Instance.WriteLine(message, t);
        } else {
            Instance.WriteLine(message);
        }
    }

    // SettingsUtils makes it easy. Just call the GetSettings extension
    // method and pass it to the input manager.
    public override void Configure(IModConfig config) {
        InputManager.Settings = config.GetSettings();
    }

    // The vanilla OWInput has a bunch of static methods which just call methods on InputManager.
    // We have the same relationship between OWMixedInput and MixedInputManager.

    public static Settings Settings => InputManager.Settings;
    public static Commands Commands => InputManager.Commands;
    public static GyroManager GyroManager => InputManager.GyroManager;

    public static bool UsingGamepadForPrimary() => InputManager.UsingGamepadForPrimary();
    public static bool LastInputUsingGamepad() => InputManager.LastInputUsingGamepad();

    public static float deltaTime => InputManager.deltaTime;
    public static float ProcessDirectInput(float value, float accumulatedValue, bool asVelocity)
        => InputManager.ProcessDirectInput(value, accumulatedValue, asVelocity);
    public static Vector2 ProcessDirectInput(Vector2 value, Vector2 accumulatedValue, bool asVelocity)
        => InputManager.ProcessDirectInput(value, accumulatedValue, asVelocity);

    public static Vector2 GetAxisValue(
        IInputCommands command,
        InputMode mask = InputMode.All,
        bool useSensitivity = true,
        bool useInversionFactor = true,
        bool includeMouse = true,
        bool includeGamepad = true,
        bool asVelocity = true,
        bool clamp = true
    )
        => InputManager.GetAxisValue(
                command,
                mask: mask,
                useSensitivity: useSensitivity,
                useInversionFactor: useInversionFactor,
                includeMouse: includeMouse,
                includeGamepad: includeGamepad,
                asVelocity: asVelocity,
                clamp: clamp);

    public static float GetValue(
        IInputCommands command,
        InputMode mask = InputMode.All,
        bool useSensitivity = true,
        bool useInversionFactor = true,
        bool includeMouse = true,
        bool includeGamepad = true,
        bool asVelocity = true,
        bool clamp = true
    )
        => InputManager.GetValue(
                command,
                mask: mask,
                useSensitivity: useSensitivity,
                useInversionFactor: useInversionFactor,
                includeMouse: includeMouse,
                includeGamepad: includeGamepad,
                asVelocity: asVelocity,
                clamp: clamp);

    public static Vector2 GetGyroValue(
        InputMode mask = InputMode.All,
        bool useSensitivity = true,
        bool asVelocity = false,
        bool clamp = false
    ) 
        => InputManager.GetGyroValue(
                mask: mask,
                useSensitivity: useSensitivity,
                asVelocity: asVelocity,
                clamp: clamp);

    public static float GetFlickValue(
        InputMode mask = InputMode.All,
        bool asVelocity = false,
        bool clamp = false
    ) 
        => InputManager.GetFlickValue(
                mask: mask,
                asVelocity: asVelocity,
                clamp: clamp);

    public bool GyroEnabled => InputManager.GyroEnabled;
    public bool FlickEnabled => InputManager.FlickEnabled;

    public static void UpdateFromAction(InputAction action) => InputManager.UpdateFromAction(action);
}

[HarmonyPatch]
public class MixedInputManager: InputManager {
    // Keep track of our mod settings.
    // If anything changed, raise an event to update cached state.
    Settings _settings;
    public Settings Settings {
        get => _settings;
        set {
            var oldValue = _settings;

            _settings = value;

            if (oldValue != value) {
                RaiseUpdateInputSettings();
                OWMixedInput.WriteLine($"Configured: {value}", MessageType.Debug);
            }

            UpdateUsingGamepad();
        }
    }

    // Keep track of the mod rebinds.
    public Commands Commands { get; private set; }

    public GyroManager GyroManager { get; } = new();

    // Called by OWMixedInput.Start
    public void Start(IModHelper helper) {
        // Initialize input bindings
        Commands = new Commands(helper.RebindingHelper);

        // Initialize gyro controls
        GyroManager.Initialize();
    }

    public void OnApplicationQuit() {
        // Cleanup SDL
        GyroManager.Quit();
    }

    // The vanilla code uses (mostly) one primary method, InputManager.UsingGamepad(), to switch
    // between gamepad and kb&m logic. In this mod, we avoid switching control methods, instead
    // favoring handling all control methods in parallel.
    //
    // The primary exception to this is the UI. The UI is either in gamepad or kb&m mode.
    // All vanilla callers of UsingGamepad either use it for UI decisions _or_ have been
    // patched to do something else instead. Thus, we override the UsingGamepad method
    // track the UI setting.
    //
    // The UsingGamepadForLastInput provides the vanilla behavior if its needed.
    //
    // The UsingGamepadForPrimary follows the user "Primary Control Scheme" setting.
    // It is used as a default value in many cases.
    
    public bool LastInputUsingGamepad() => base.UsingGamepad();

    public bool UsingGamepadForPrimary()
        => Settings.controlScheme.Match(
            Vanilla: LastInputUsingGamepad(),
            Gamepad: true,
            KBM: false
        );

    // Override the vanilla UsingGamepad method to follow the UI setting.
    //
    // If no gamepad is connected (i.e. !IsGamepadEnabled), return false.
    // This prevents users from getting stuck in the controller interface without a controller.
    public override bool UsingGamepad() => 
        IsGamepadEnabled()
        && Settings.userInterface.Match(
            Default: UsingGamepadForPrimary(),
            Vanilla: LastInputUsingGamepad(),
            Gamepad: true,
            KBM: false
        );

    // In this mod, we accumulate input in Update methods, and it is processed
    // in both Update and FixedUpdate methods. Unfortunately, the deltaTime of a
    // FixedUpdate is not the sum of the deltaTimes of the Updates preceeding it.
    //
    // Therefore, to reasonably convert accumulated inputs to velocities, we need to
    // keep track of the cumulative Update time between FixedUpdates to use as a denominator.
    //
    // This is reported via the deltaTime property.
    //
    // Get the input deltaTime. Returns the accumulated time in FixedUpdate, and otherwise
    // returns unscaledDeltaTime.
    public float deltaTime => Time.inFixedTimeStep ? _accumulatedTime : Time.unscaledDeltaTime;

    // Sometimes, FixedUpdates happen with no Updates in between. In this case,
    // the _staleAccumulation property indicates that currently accumulated values are stale.
    //
    // Whether or not inputs are stale does not matter for velocity inputs, but it does matter
    // for direct inputs like mouse/gyro. Two consecutive FixedUpdates should not duplicate
    // mouse inputs.
    //
    // These methods processes direct inputs, taking both the most recent value and a value
    // accumulated across consecutive Updates.
    public float ProcessDirectInput(float value, float accumulatedValue, bool asVelocity) {
        float result;
        if (Time.inFixedTimeStep) {
            if (asVelocity || !_staleAccumulation) {
                result = accumulatedValue;
            } else {
                result = 0f;
            }
        } else {
            result = value;
        }

        if (asVelocity) {
            result /= deltaTime;
        }

        return result;
    }
    public Vector2 ProcessDirectInput(Vector2 value, Vector2 accumulatedValue, bool asVelocity)
        => new Vector2(
            ProcessDirectInput(value.x, accumulatedValue.x, asVelocity),
            ProcessDirectInput(value.y, accumulatedValue.y, asVelocity));

    // State for keeping track of accumulation.
    float _accumulatedTime = 0f;
    bool _clearAccumulation = false;
    bool _staleAccumulation = false;

    // Bit field of input types encountered this frame.
    InputConsts.InputType _currentFrameInputType;

    public override void Update() {
        //
        // Time accumulation
        //
        _staleAccumulation = false;
        if (_clearAccumulation) {
            _accumulatedTime = 0f;
        }
        _clearAccumulation = false;
        _accumulatedTime += Time.unscaledDeltaTime;

        // No need to update the mouse anymore, we aren't using it like this.
        //// Vanilla
        //UpdateMouse();

        // Reset input types encountered this frame.
        _currentFrameInputType = InputConsts.InputType.None;

        // Request Unity update our InputActions.
        InputSystem.Update();

        // Update the command manager.
        // Commands which update now call our new UpdateFromAction method.
        commandManager.Update();

        // If we encounterd an input this frame, set the LastCommandInputType.
        if (_currentFrameInputType != InputConsts.InputType.None) {
            LastCommandInputType = _currentFrameInputType;
        }

        // Vanilla forgets to call this, but we use it for IsGamepadEnabled.
        RefreshLastInputType();

        // Update cached _usingGamepad flag.
        UpdateUsingGamepad();

        // Like vanilla, accept the pending input mode and raise an event if necessary.
        if (BaseInputManager._currentInputMode != BaseInputManager._pendingInputMode) {
            BaseInputManager._currentInputMode = BaseInputManager._pendingInputMode;

            RaiseUpdateInputMode();
        }

        // Update the gyro manager
        GyroManager.Update();
    }
    public override void FixedUpdate() {
        //
        // Time accumulation
        //
        if (_clearAccumulation) {
            _staleAccumulation = true;
        }
        _clearAccumulation = true;

        base.FixedUpdate();

        // Update the gyro manager
        GyroManager.FixedUpdate();
    }

    // Update the cached _usingGamepad flag and raise UpdateInputDevice if changed.
    void UpdateUsingGamepad() {
        var usingGamepad = UsingGamepad();
        if (_usingGamepad != usingGamepad) {
            _usingGamepad = usingGamepad;

            RaiseUpdateInputDevice();
        }
    }

    // The vanilla behavior calls OWInput.UpdateLastCommandType in the performed callback of each IInputAction.
    // This updates the games input type according to the performed action. This can result in multiple calls
    // per-frame and therefore multiple invocations of the vanilla UpdateInputDevice event per-frame.
    //
    // Instead, we have the IInputAction update methods call this method instead, using it to update
    // the _currentFrameInputDevice. This value is then read in the MixedInputManager Update method.
    public void UpdateFromAction(InputAction action) {
        var activeControl = action.activeControl;
        if (activeControl == null || !activeControl.IsActuated(0.3f)) {
            return;
        }

        if (activeControl.device is Gamepad gamepad) {
            _currentFrameInputType |= InputConsts.InputType.GamePad;
            if (Gamepad.current != gamepad) {
                // Is this necessary?
                // This is vanilla does this but I'm not sure why.
                gamepad.MakeCurrent();
            }
            _currentGamepad = gamepad;
        } else if (activeControl.device is Keyboard) {
            _currentFrameInputType |= InputConsts.InputType.Keyboard;
        } else if (activeControl.device is Mouse) {
            _currentFrameInputType |= InputConsts.InputType.Mouse;
        }
    }
    // The old behavior is now unsupported.
    public override void UpdateLastInputCommandType(InputAction.CallbackContext context) {
        throw new Exception("MixedInputManager.UpdateLastInputCommandType");
    }

    // The vanilla game fades input unconditionally.
    // We control this behavior with a user setting.
    public float InputFadeFraction
        => Settings.inputFade ? _inputFadeFraction : 1f;

    // The vanilla InputManager has GetAxisValue and GetValue methods
    // which support masking and input fading. We provide similar methods
    // with the additional granularity of our modded input commands.
    public Vector2 GetAxisValue(
        IInputCommands command,
        InputMode mask = InputMode.All,
        bool useSensitivity = true,
        bool useInversionFactor = true,
        bool includeMouse = true,
        bool includeGamepad = true,
        bool asVelocity = true,
        bool clamp = true
    ) {
        if (IsInputMode(mask)) {
            return command.GetAxisValue(
                    useSensitivity: useSensitivity,
                    useInversionFactor: useInversionFactor,
                    includeMouse: includeMouse,
                    includeGamepad: includeGamepad,
                    asVelocity: asVelocity,
                    clamp: clamp
                ) 
                * InputFadeFraction;
        } else {
            return Vector2.zero;
        }
    }
    public float GetValue(
        IInputCommands command,
        InputMode mask = InputMode.All,
        bool useSensitivity = true,
        bool useInversionFactor = true,
        bool includeMouse = true,
        bool includeGamepad = true,
        bool asVelocity = true,
        bool clamp = true
    ) {
        if (IsInputMode(mask)) {
            return command.GetValue(
                    useSensitivity: useSensitivity,
                    useInversionFactor: useInversionFactor,
                    includeMouse: includeMouse,
                    includeGamepad: includeGamepad,
                    asVelocity: asVelocity,
                    clamp: clamp
                )
                * InputFadeFraction;
        } else {
            return 0f;
        }
    }

    // Override the vanilla methods to use our new InputFadeFraction.
    // Since the vanilla methods are not virtual, we use Harmony patches.
    [HarmonyPrefix, HarmonyPatch(typeof(InputManager), nameof(InputManager.GetAxisValue))]
    static bool InputManager_GetAxisValue_Patch(
        InputManager __instance, 
        IInputCommands command,
        InputMode mask, 
        ref Vector2 __result, 
        MethodBase __originalMethod
    ) {
        Check_InputManager_Unpatched(__instance, __originalMethod);

        var manager = (MixedInputManager)__instance;
        __result = manager.GetAxisValue(command, mask,
                useSensitivity: true,
                useInversionFactor: true,
                includeMouse: true,
                includeGamepad: true,
                asVelocity: true,
                clamp: true);
        return false;
    }
    [HarmonyPrefix, HarmonyPatch(typeof(InputManager), nameof(InputManager.GetValue))]
    static bool InputManager_GetValue_Patch(
        InputManager __instance, 
        IInputCommands command,
        InputMode mask, 
        ref float __result, 
        MethodBase __originalMethod
    ) {
        Check_InputManager_Unpatched(__instance, __originalMethod);

        var manager = (MixedInputManager)__instance;
        __result = manager.GetValue(command, mask,
                useSensitivity: true,
                useInversionFactor: true,
                includeMouse: true,
                includeGamepad: true,
                asVelocity: true,
                clamp: true);
        return false;
    }

    // We provide the same wrapping behavior for GetGyroValue.
    // GetFlickValue does not get the input fade fraction because
    // of the nature of the input method.
    public Vector2 GetGyroValue(
        InputMode mask = InputMode.All,
        bool useSensitivity = true,
        bool asVelocity = false,
        bool clamp = false
    ) {
        if (IsInputMode(mask)) {
            return GyroManager.GetGyroValue(
                    useSensitivity: useSensitivity,
                    asVelocity: asVelocity,
                    clamp: clamp
                )
                * InputFadeFraction;
        } else {
            return Vector2.zero;
        }
    }
    public float GetFlickValue(
        InputMode mask = InputMode.All,
        bool asVelocity = false,
        bool clamp = false
    ) {
        if (IsInputMode(mask)) {
            return GyroManager.GetFlickValue(
                    asVelocity: asVelocity,
                    clamp: clamp);
        } else {
            return 0f;
        }
    }

    // Delegate to GyroManager to check if gyro and flick stick are enabled.
    public bool GyroEnabled => GyroManager.GyroEnabled;
    public bool FlickEnabled => GyroManager.FlickEnabled;

    // The vanilla InputManager has three events we need to care about:
    // OnUpdateInputDevice, OnUpdateUsingGamepadChange, and OnUpdateInputMode.
    //
    // OnUpdateInputMode is raised when the input mode changes. Easy enough.
    //
    // In vanilla, OnUpdateInputDevice is raised LastCommandInputType changes.
    // However, the only reason classes subscribe to this event is to update cached
    // values of UsingGamepad. Therefore, we instead raise OnUpdateInputDevice when
    // UsingGamepad changes, i.e. when the UI style should change.
    //
    // In vanilla, the poorly named OnUpdateUsingGamepadChange is raised when
    // UsingGamepad changes. This is only used to update the sensitivities in
    // PlayerData, which we don't need to do in this mod. Thus, this event
    // is deprecated and will not be raised.
    //
    // We also define our own event: OnUpdateInputConfig, for when the mod settings
    // are changed.
    public delegate void UpdateInputSettingsEvent();

    public event UpdateInputSettingsEvent OnUpdateInputSettings;

    public void RaiseUpdateInputSettings() {
        OnUpdateInputSettings?.Invoke();
    }
    // We cannot directly refer to the events of the base class, but we still want to be able to raise them,
    // so we harmony patch our own methods to get access to the hidden event fields.
    public void RaiseUpdateInputMode() {}
    [HarmonyPostfix, HarmonyPatch(typeof(MixedInputManager), nameof(MixedInputManager.RaiseUpdateInputMode))]
    static void RaiseUpdateInputMode_Postfix(UpdateInputDeviceEvent ___OnUpdateInputMode) {
        ___OnUpdateInputMode?.Invoke();
    }
    public void RaiseUpdateInputDevice() {}
    [HarmonyPostfix, HarmonyPatch(typeof(MixedInputManager), nameof(MixedInputManager.RaiseUpdateInputDevice))]
    static void RaiseUpdateInputDevice_Postfix(UpdateInputDeviceEvent ___OnUpdateInputDevice) {
        ___OnUpdateInputDevice?.Invoke();
    }
    public void RaiseUsingGamepadChange() {}
    [HarmonyPostfix, HarmonyPatch(typeof(MixedInputManager), nameof(MixedInputManager.RaiseUsingGamepadChange))]
    static void RaiseUsingGamepadChange_Postfix(UsingGamepadChangeEvent ___OnUpdateUsingGamepadChange) {
        ___OnUpdateUsingGamepadChange?.Invoke();
    }

    // We throw errors if the important vanilla methods are called on an unpatched InputManager.
    // Any exceptions here are a result of a bug in the mod, likely caused if we are not injected
    // soon enough.
    [HarmonyPrefix]
    [HarmonyPatch(typeof(InputManager), nameof(InputManager.Awake))]
    [HarmonyPatch(typeof(InputManager), nameof(InputManager.UsingGamepad))]
    [HarmonyPatch(typeof(InputManager), nameof(InputManager.Update))]
    [HarmonyPatch(typeof(InputManager), nameof(InputManager.FixedUpdate))]
    static void Check_InputManager_Unpatched(InputManager __instance, MethodBase __originalMethod) {
        if (__instance is not MixedInputManager) {
            throw new Exception($"{__originalMethod} called on unpatched InputManager.");
        }
    }
}
