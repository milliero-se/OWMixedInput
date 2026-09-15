using OWML.Common;
using OWML.Common.Enums;

using UnityEngine.InputSystem;

namespace OWMixedInput;

public record struct Commands {
    InputConsts.InputCommandType gyro;
    public IInputCommands Gyro => InputLibrary.GetInputCommand(gyro);

    InputConsts.InputCommandType calibrate;
    public IInputCommands Calibrate => InputLibrary.GetInputCommand(calibrate);

    InputConsts.InputCommandType recenter;
    public IInputCommands Recenter => InputLibrary.GetInputCommand(recenter);

    public Commands(IRebindingHelper rebindingHelper) {
        gyro = rebindingHelper.RegisterRebindable(
            "Gyro",
            "Command used for enabling/disabling gyro based on the 'Gyro Button Mode' setting. Defaults to 'None'. Has no effect unless the gyro camera is enabled.",
            Key.None, GamepadBinding.None, axis: false);

        calibrate = rebindingHelper.RegisterRebindable(
            "Calibrate",
            "Command used to calibrate the gyro. Lay the controller on a flat surface and hold this button down for a second or two to correct drift. Affected by the 'Calibrate Button Mode' setting. Defaults to 'None'. Has no effect unless the gyro camera is enabled.",
            Key.None, GamepadBinding.None, axis: false);

        recenter = rebindingHelper.RegisterRebindable(
            "Recenter Camera",
            "Command used to recenter the camera Y-axis when controlling the player. Defaults to 'None'. This command does not require the gyro camera to function.",
            Key.None, GamepadBinding.None, axis: false);
    }
}



