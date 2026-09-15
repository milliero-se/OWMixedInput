using GyroHelpers;
using GyroHelpers.GyroSpaces;

using SDL_Gamepad = System.IntPtr;

using UnityEngine;

namespace OWMixedInput;

public class GyroState {
    public SDL_Gamepad gamepad { get; }

    public GyroInput GyroInput { get; } = new();
    public GyroProcessor GyroProcessor { get; } = new();

    public float BiasCalibrationTime = 0f;

    public GyroState(IntPtr gamepad) {
        this.gamepad = gamepad;
    }

    public void UpdateSettings(Settings settings) {
        // Convert from the Settings.GyroSpace enum to IGyroSpace.
        GyroProcessor.GyroSpace = settings.gyroSpace.Match<IGyroSpace>(
            LocalYaw: new LocalGyroSpace(),
            LocalRoll: new LocalGyroSpace(GyroAxis.Roll),
            PlayerTurn: new PlayerTurnGyroSpace(),
            PlayerLean: new PlayerLeanGyroSpace()
        );

        // Convert smoothingThreshold from deg/s to rad/s
        GyroProcessor.SmoothingThresholdDirect = settings.gyroSmoothingThreshold
            * MixedInputUtils.DegToRad;
        // We don't expose separate thresholds, so same value
        GyroProcessor.SmoothingThresholdSmooth = settings.gyroSmoothingThreshold
            * MixedInputUtils.DegToRad;

        // Convert smoothingTime from ms to s.
        GyroProcessor.SmoothingTime = settings.gyroSmoothingTime / 1000f;

        // Convert tighteningThreshold from deg/s to rad/s.
        GyroProcessor.TighteningThreshold = settings.gyroTighteningThreshold
            * MixedInputUtils.DegToRad;
       
        // Convert acceleration thresholds from deg/s to rad/s
        GyroProcessor.Acceleration.ThresholdSlow = settings.gyroAccelerationThresholdSlow
            * MixedInputUtils.DegToRad;
        GyroProcessor.Acceleration.ThresholdFast = settings.gyroAccelerationThresholdFast
            * MixedInputUtils.DegToRad;
        GyroProcessor.Acceleration.SensitivitySlow = settings.gyroAccelerationSensitivitySlow;
        GyroProcessor.Acceleration.SensitivityFast = settings.gyroAccelerationSensitivityFast;
    }

    public void Begin()
        => GyroInput.Begin();

    public void AddGyroSample(Vector3 gyro, ulong timestamp)
        => GyroInput.AddGyroSample(gyro.ToSystem(), timestamp);
    public void AddAccelSample(Vector3 accel, ulong timestamp)
        => GyroInput.AddAccelerometerSample(accel.ToSystem(), timestamp);

    public Vector2 UpdateGyro(float deltaTime) {
        // Move to gyro manager?
        if (BiasCalibrationTime > 0) {
            BiasCalibrationTime -= deltaTime;
            GyroInput.Calibrating = BiasCalibrationTime > 0;
        }

        return GyroProcessor.Update(GyroInput.Gyro, deltaTime).ToUnity();
    }

    public void Reset() {
        GyroProcessor.Reset();
    }
}
