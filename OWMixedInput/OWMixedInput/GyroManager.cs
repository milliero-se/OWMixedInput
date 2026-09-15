using OWML.Common;

using static SDL3.SDL;
using SDL_JoystickID = uint;
using SDL_Gamepad = System.IntPtr;

using GyroHelpers;

using UnityEngine;

namespace OWMixedInput;

// Manages Gyro and FlickStick.
public class GyroManager {
   
    // Store a gyro state for each connected controller.
    Dictionary<SDL_JoystickID, GyroState> controllers = new();

    // Store a single FlickStick state.
    FlickStick flickStick { get; } = new();

    // Is gyro enabled?
    public bool GyroEnabled { get; private set; }
    // Is flick stick enabled?
    public bool FlickEnabled { get; private set; }

    // State of the gyro toggle
    bool _gyroToggle;

    // Do we need to clear accumulated inputs?
    bool _clearAccumulation = false;
    // Gyro value from current frame
    Vector2 _gyroValue;
    // Gyro value accumulated across fixed update
    Vector2 _gyroAccumulated;
    // Flick stick value from current frame
    float _flickValue; 
    // Flick stick value accumulated across fixed update
    float _flickAccumulated;

    public Vector2 GetGyroValue(
        bool useSensitivity = true,
        bool asVelocity = false,
        bool clamp = false
    ) {
        Vector2 result = OWMixedInput.ProcessDirectInput(_gyroValue, _gyroAccumulated, asVelocity);

        if (useSensitivity) {
            result *= OWMixedInput.Settings.gyroSensitivity;
        }

        if (asVelocity && clamp) {
            MixedInputUtils.ClampMagnitude(ref result);
        }

        return result;
    }

    public float GetFlickValue(
        bool asVelocity = false,
        bool clamp = false
    ) {
        float result = OWMixedInput.ProcessDirectInput(_flickValue, _flickAccumulated, asVelocity);

        if (asVelocity && clamp) {
            MixedInputUtils.ClampMagnitude(ref result);
        }

        return result;
    }

    // Our update routine requires our custom command rebindables
    // to be loaded. We keep track of that here.
    bool commandsInitialized = false;
    void OnInputCommandsInitialized() {
        commandsInitialized = true;
    }

    // Initialize SDL and register settings event handler.
    public void Initialize() {
        if (!SDL_Init(SDL_InitFlags.SDL_INIT_GAMEPAD)) {
            throw new Exception($"Unable to initialize SDL3: {SDL_GetError()}");
        }
        SDL_SetGamepadEventsEnabled(true);

        int version = SDL_GetVersion();
        int major = version / 1000000;
        int minor = (version / 1000) % 1000;
        int patch = version % 1000;

        string revision = SDL_GetRevision();

        OWMixedInput.WriteLine($"SDL3 version {major}.{minor}.{patch} successfully initialized!", MessageType.Success);
        OWMixedInput.WriteLine($"SDL3 revision: {revision}", MessageType.Debug);

        // Add settings event handler
        OWMixedInput.InputManager.OnUpdateInputSettings += UpdateSettings;
        // Add command initialization event handler
        OWMixedInput.InputManager.commandManager.OnInputCommandsInitialized += OnInputCommandsInitialized;

        UpdateSettings();

    }

    // Cleanup SDL resources.
    // Called by InputManager.OnApplicationQuit
    public void Quit() {
        SDL_Quit();
    }

    public void UpdateSettings() {
        var settings = OWMixedInput.Settings;

        GyroEnabled = settings.gyroEnabled;
        FlickEnabled = settings.flickEnabled;

        // Reset the toggle if we aren't using it
        if (!settings.gyroButtonMode.isToggle) {
            _gyroToggle = false;
        }

        // Update gyro settings on each controller
        foreach (var state in controllers.Values) {
            state.UpdateSettings(settings);
        }

        //
        // Update flick stick settings
        //

        flickStick.FlickThreshold = settings.flickThreshold;
        // Convert flickTime from ms to s
        flickStick.FlickTime = settings.flickTime / 1000f;

        // Convert Settings.FlickSnapping to GyroHelpers.FlickSnapping
        flickStick.Snapping = settings.flickSnapping.Match(
            None: FlickSnapping.None,
            One: FlickSnapping.ForwardOnly,
            Two: FlickSnapping.Two,
            Four: FlickSnapping.Four,
            Six: FlickSnapping.Six,
            Eight: FlickSnapping.Eight
        );

        // We don't expose snappingStrength currently.
        flickStick.SnappingStrength = 1f;

        // Convert forwardDeadzone from deg to rad
        flickStick.SnappingForwardDeadzone = settings.flickFowardDeadzone
            * MixedInputUtils.DegToRad;
        
        // Convert smoothingThreshold from deg to rad
        flickStick.SmoothingThresholdDirect = settings.flickSmoothingThreshold
            * MixedInputUtils.DegToRad;
        // We don't expose separate thresholds, so same value
        flickStick.SmoothingThresholdSmooth = settings.flickSmoothingThreshold
            * MixedInputUtils.DegToRad;

        // Convert smoothingTime from ms to s
        flickStick.SmoothingTime = settings.flickSmoothingTime / 1000f;
    }

    public void Update() {
        // Wait until the commands are initialized.
        if (!commandsInitialized) { return; }

        //
        // Gyro
        //
        if (GyroEnabled && Application.isFocused) {
            bool calibrating = OWMixedInput.Settings.gyroCalibrateButtonMode.Match(
                Press: OWInput.IsPressed(OWMixedInput.Commands.Calibrate),
                Hold: OWInput.IsPressed(OWMixedInput.Commands.Calibrate, minPressDuration: 0.25f),
                Menu: OWInput.IsInputMode(InputMode.Menu) && OWInput.IsPressed(OWMixedInput.Commands.Calibrate)
            );

            if (calibrating) {
                foreach (var state in controllers.Values) {
                    // Give 100ms of calibration time.
                    state.BiasCalibrationTime = 0.1f;
                }
            }

            // Prepare for SDL polling
            Begin();
            // SDL poll
            Poll();

            // Update the gyro value
            _gyroValue = UpdateGyro(Time.unscaledDeltaTime);

            // Update the gyro toggle
            if (OWMixedInput.Settings.gyroButtonMode.isToggle
                && OWInput.IsNewlyPressed(OWMixedInput.Commands.Gyro))
            {
                _gyroToggle = !_gyroToggle;
            }

            var gyroPressed = OWInput.IsPressed(OWMixedInput.Commands.Gyro);
           
            // Is gyro is masked according to the user setting, reset the gyro value
            // back to zero. We needed to have called UpdateGyro even if masked as 
            // it is responsible for calibration logic.
            bool gyroIsMasked = OWMixedInput.Settings.gyroButtonMode.Match(
                Disabled: false,
                Hold: !gyroPressed,
                Mute: gyroPressed,
                Toggle: !_gyroToggle
            );
            if (gyroIsMasked) {
                _gyroValue = Vector2.zero;
            }

        } else {
            ResetGyro();
            _gyroValue = Vector2.zero;
        }

        //
        // Flick stick
        //
        if (FlickEnabled) {
            // Should we useInversionFactor: false here?
            //
            // idk why we need to invert the y-axis, this seems like a bug in GyroHelpers to me.
            var stick = OWMixedInput.GetAxisValue(InputLibrary.look, 
                    includeMouse: false, useSensitivity: false, clamp: false)
                    * new Vector2(1f, -1f);

            _flickValue = UpdateFlickStick(stick, Time.unscaledDeltaTime);
        } else {
            ResetFlickStick();
            _flickValue = 0f;
        }
      
        //
        // Accumulate
        //
        if (_clearAccumulation) {
            _gyroAccumulated = Vector2.zero;
            _flickAccumulated = 0f;
        }
        _clearAccumulation = false;

        _gyroAccumulated += _gyroValue;
        _flickAccumulated += _flickValue;
    }

    public void FixedUpdate() {
        _clearAccumulation = true;
    }

    // Prepare gyro states for new sensor samples
    void Begin() {
        foreach (var state in controllers.Values) {
            state.Begin();
        }
    }

    // Poll SDL for sensor events
    void Poll() {
		SDL_Event evnt;
		while (SDL_PollEvent(out evnt)) {
			switch ((SDL_EventType)evnt.type) {
				case SDL_EventType.SDL_EVENT_GAMEPAD_ADDED:
					OnControllerAdded(evnt.gdevice);
					break;
				case SDL_EventType.SDL_EVENT_GAMEPAD_REMOVED:
					OnControllerRemoved(evnt.gdevice);
					break;
				case SDL_EventType.SDL_EVENT_GAMEPAD_SENSOR_UPDATE:
					OnControllerSensorUpdate(evnt.gsensor);
					break;
				case SDL_EventType.SDL_EVENT_GAMEPAD_BUTTON_DOWN:
				case SDL_EventType.SDL_EVENT_GAMEPAD_BUTTON_UP:
					OnControllerButtonUpdate(evnt.gbutton);
					break;
			}
		}
	}

    public Vector2 UpdateGyro(float deltaTime) {
        var value = Vector2.zero;

        foreach (var state in controllers.Values) {
            value += state.UpdateGyro(deltaTime);
        }

        return value;
    }
    void ResetGyro() {
        foreach (var state in controllers.Values) {
            state.Reset();
        }
    }

    float UpdateFlickStick(Vector2 stick, float deltaTime)
        => flickStick.Update(stick.ToSystem(), deltaTime);
    void ResetFlickStick()
        => flickStick.Reset();

	void OnControllerAdded(SDL_GamepadDeviceEvent evnt) {
        SDL_JoystickID id = evnt.which;
        var gamepad = SDL_OpenGamepad(id);
        if (gamepad == null) {
            // There was an error.
            return;
        }

        var hasGyro = SDL_GamepadHasSensor(gamepad, SDL_SensorType.SDL_SENSOR_GYRO);
        if (!hasGyro) {
            // We only care about controllers with gyro.
            SDL_CloseGamepad(gamepad);
            return;
        }

        // Enable gyro and accel on the controller
        SDL_SetGamepadSensorEnabled(gamepad, SDL_SensorType.SDL_SENSOR_GYRO, true);
        SDL_SetGamepadSensorEnabled(gamepad, SDL_SensorType.SDL_SENSOR_ACCEL, true);

        // Initialize new state for the controller.
        var state = new GyroState(gamepad);
       
        if (OWMixedInput.Settings.gyroCalibrateOnConnect) {
            // Calibrate the gyro right away.
            state.BiasCalibrationTime = 1f;
        }

        // Initialize new state for the controller and add it to dictionary
        controllers.Add(id, state);
	}

	void OnControllerRemoved(SDL_GamepadDeviceEvent evnt) {
        if (!controllers.TryGetValue(evnt.which, out var state)) {
            // Controller never existed?
            return;
        }
        // Remove the controller from the state dictionary
        controllers.Remove(evnt.which);

        // Out of an abundance of caution.
        SDL_CloseGamepad(state.gamepad);
	}

	void OnControllerSensorUpdate(SDL_GamepadSensorEvent evnt) {
        if (!GyroEnabled) {
            // Skip for performance
            return;
        }

        if (!controllers.TryGetValue(evnt.which, out var state)) {
            // Controller doesn't exist?
            return;
        }

        var type = (SDL_SensorType)evnt.sensor;
        
        // timestamp in nanos
        var timestamp = evnt.sensor_timestamp; 

        // Convert float[3] to a Vector3
        Vector3 data;
        unsafe { data = *(Vector3*)evnt.data; }

        // Update the controller state with the sensor sample.
        switch (type) {
            case SDL_SensorType.SDL_SENSOR_GYRO:
                state.AddGyroSample(data, timestamp);
                break;
            case SDL_SensorType.SDL_SENSOR_ACCEL:
                state.AddAccelSample(data, timestamp);
                break;
        }
	}

	void OnControllerButtonUpdate(SDL_GamepadButtonEvent evnt) {
        // Unused
        // In the future, we could use this for active controller detection.
	}
}
