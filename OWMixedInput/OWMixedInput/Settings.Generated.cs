// NOTE: This file is auto-generated.
// See Generate_Settings_cs.py for details.

using System.ComponentModel;

namespace OWMixedInput;

public record struct Settings {
    public enum ControlScheme {
        [Description("Last Input")]
        Vanilla,
        [Description("Gamepad")]
        Gamepad,
        [Description("KB&M")]
        KBM,
    }
    [Description("ControlScheme")]
    public ControlScheme controlScheme;

    public enum UserInterface {
        [Description("Default")]
        Default,
        [Description("Last Input")]
        Vanilla,
        [Description("Gamepad")]
        Gamepad,
        [Description("KB&M")]
        KBM,
    }
    [Description("UserInterface")]
    public UserInterface userInterface;

    public enum Cursor {
        [Description("Default")]
        Default,
        [Description("Last Input")]
        Vanilla,
        [Description("On")]
        Yes,
        [Description("Off")]
        No,
    }
    [Description("Cursor")]
    public Cursor cursor;

    public enum VirtualKeyboard {
        [Description("Steam Deck")]
        Vanilla,
        [Description("Big Picture Mode")]
        Yes,
        [Description("Off")]
        No,
    }
    [Description("VirtualKeyboard")]
    public VirtualKeyboard virtualKeyboard;

    public enum Rumble {
        [Description("Default")]
        Default,
        [Description("Last Input")]
        Vanilla,
        [Description("Always")]
        Yes,
        [Description("Never")]
        No,
    }
    [Description("Rumble")]
    public Rumble rumble;

    [Description("MouseSmoothing")]
    public bool mouseSmoothing;

    public enum MouseSensitivity {
        [Description("Mouse")]
        Mouse,
        [Description("Joystick")]
        Joystick,
    }
    [Description("MouseSensitivity")]
    public MouseSensitivity mouseSensitivity;

    public enum RollAxis {
        [Description("Yaw")]
        Yaw,
        [Description("X")]
        X,
        [Description("Up/down")]
        Y,
    }
    [Description("RollAxis")]
    public RollAxis rollAxis;

    [Description("InvertRoll")]
    public bool invertRoll;

    [Description("SquareifyXZ")]
    public bool squareifyXZ;

    [Description("SquareifyPitchYaw")]
    public bool squareifyPitchYaw;

    [Description("ExtendRotationalThrusters")]
    public bool extendRotationalThrusters;

    [Description("GamepadSensitivityRatio")]
    public float gamepadSensitivityRatio;

    [Description("MouseSensitivityRatio")]
    public float mouseSensitivityRatio;

    [Description("FreeLook")]
    public bool freeLook;

    [Description("SlowCamera")]
    public bool slowCamera;

    [Description("RecenterTime")]
    public float recenterTime;

    [Description("GyroEnabled")]
    public bool gyroEnabled;

    [Description("GyroSensitivity")]
    public float gyroSensitivity;

    [Description("GyroSensitivityRatio")]
    public float gyroSensitivityRatio;

    public enum GyroSpace {
        [Description("Local Yaw")]
        LocalYaw,
        [Description("Local Roll")]
        LocalRoll,
        [Description("Player Turn")]
        PlayerTurn,
        [Description("Player Lean")]
        PlayerLean,
    }
    [Description("GyroSpace")]
    public GyroSpace gyroSpace;

    public enum GyroButtonMode {
        [Description("Disabled")]
        Disabled,
        [Description("Hold")]
        Hold,
        [Description("Mute")]
        Mute,
        [Description("Toggle")]
        Toggle,
    }
    [Description("GyroButtonMode")]
    public GyroButtonMode gyroButtonMode;

    public enum GyroCalibrateButtonMode {
        [Description("In Menus")]
        Menu,
        [Description("Press")]
        Press,
        [Description("Hold")]
        Hold,
    }
    [Description("GyroCalibrateButtonMode")]
    public GyroCalibrateButtonMode gyroCalibrateButtonMode;

    [Description("GyroCalibrateOnConnect")]
    public bool gyroCalibrateOnConnect;

    [Description("GyroTighteningThreshold")]
    public float gyroTighteningThreshold;

    [Description("GyroSmoothingThreshold")]
    public float gyroSmoothingThreshold;

    [Description("GyroSmoothingTime")]
    public float gyroSmoothingTime;

    [Description("GyroAccelerationThresholdSlow")]
    public float gyroAccelerationThresholdSlow;

    [Description("GyroAccelerationThresholdFast")]
    public float gyroAccelerationThresholdFast;

    [Description("GyroAccelerationSensitivitySlow")]
    public float gyroAccelerationSensitivitySlow;

    [Description("GyroAccelerationSensitivityFast")]
    public float gyroAccelerationSensitivityFast;

    [Description("FlickEnabled")]
    public bool flickEnabled;

    [Description("FlickThreshold")]
    public float flickThreshold;

    [Description("FlickTime")]
    public float flickTime;

    public enum FlickSnapping {
        [Description("None")]
        None,
        [Description("One")]
        One,
        [Description("Two")]
        Two,
        [Description("Four")]
        Four,
        [Description("Six")]
        Six,
        [Description("Eight")]
        Eight,
    }
    [Description("FlickSnapping")]
    public FlickSnapping flickSnapping;

    [Description("FlickFowardDeadzone")]
    public float flickFowardDeadzone;

    [Description("FlickSmoothingThreshold")]
    public float flickSmoothingThreshold;

    [Description("FlickSmoothingTime")]
    public float flickSmoothingTime;

    [Description("HelmetShake")]
    public bool helmetShake;

    [Description("InputFade")]
    public bool inputFade;

    public enum ProbeOverlaps {
        [Description("Primary")]
        Primary,
        [Description("Both")]
        Both,
    }
    [Description("ProbeOverlaps")]
    public ProbeOverlaps probeOverlaps;

    [Description("SpaceWalkRangeX")]
    public float spaceWalkRangeX;

    [Description("SpaceWalkRangeY")]
    public float spaceWalkRangeY;

    public enum ProbeLauncher {
        [Description("Default")]
        Default,
        [Description("Last Input")]
        Vanilla,
        [Description("Hybrid")]
        Hybrid,
        [Description("Gamepad")]
        Gamepad,
        [Description("KB&M")]
        KBM,
    }
    [Description("ProbeLauncher")]
    public ProbeLauncher probeLauncher;

    public enum Autopilot {
        [Description("Default")]
        Default,
        [Description("Last Input")]
        Vanilla,
        [Description("Gamepad")]
        Gamepad,
        [Description("KB&M")]
        KBM,
    }
    [Description("Autopilot")]
    public Autopilot autopilot;

    public enum ShipLog {
        [Description("Default")]
        Default,
        [Description("Last Input")]
        Vanilla,
        [Description("Gamepad")]
        Gamepad,
        [Description("KB&M")]
        KBM,
    }
    [Description("ShipLog")]
    public ShipLog shipLog;

    public enum RoastingStick {
        [Description("Default")]
        Default,
        [Description("Last Input")]
        Vanilla,
        [Description("Gamepad")]
        Gamepad,
        [Description("KB&M")]
        KBM,
    }
    [Description("RoastingStick")]
    public RoastingStick roastingStick;

}

public static class SettingsExtensions {
    public static TResult Match<TResult>(this Settings.ControlScheme controlScheme, TResult Vanilla, TResult Gamepad, TResult KBM)
        => controlScheme switch {
            Settings.ControlScheme.Vanilla => Vanilla,
            Settings.ControlScheme.Gamepad => Gamepad,
            Settings.ControlScheme.KBM => KBM,
            _ => throw new Unreachable()
        };

    extension(Settings.ControlScheme controlScheme) {
        public bool isVanilla => controlScheme == Settings.ControlScheme.Vanilla;
        public bool isGamepad => controlScheme == Settings.ControlScheme.Gamepad;
        public bool isKBM => controlScheme == Settings.ControlScheme.KBM;
    }

    public static TResult Match<TResult>(this Settings.UserInterface userInterface, TResult Default, TResult Vanilla, TResult Gamepad, TResult KBM)
        => userInterface switch {
            Settings.UserInterface.Default => Default,
            Settings.UserInterface.Vanilla => Vanilla,
            Settings.UserInterface.Gamepad => Gamepad,
            Settings.UserInterface.KBM => KBM,
            _ => throw new Unreachable()
        };

    extension(Settings.UserInterface userInterface) {
        public bool isDefault => userInterface == Settings.UserInterface.Default;
        public bool isVanilla => userInterface == Settings.UserInterface.Vanilla;
        public bool isGamepad => userInterface == Settings.UserInterface.Gamepad;
        public bool isKBM => userInterface == Settings.UserInterface.KBM;
    }

    public static TResult Match<TResult>(this Settings.Cursor cursor, TResult Default, TResult Vanilla, TResult Yes, TResult No)
        => cursor switch {
            Settings.Cursor.Default => Default,
            Settings.Cursor.Vanilla => Vanilla,
            Settings.Cursor.Yes => Yes,
            Settings.Cursor.No => No,
            _ => throw new Unreachable()
        };

    extension(Settings.Cursor cursor) {
        public bool isDefault => cursor == Settings.Cursor.Default;
        public bool isVanilla => cursor == Settings.Cursor.Vanilla;
        public bool isYes => cursor == Settings.Cursor.Yes;
        public bool isNo => cursor == Settings.Cursor.No;
    }

    public static TResult Match<TResult>(this Settings.VirtualKeyboard virtualKeyboard, TResult Vanilla, TResult Yes, TResult No)
        => virtualKeyboard switch {
            Settings.VirtualKeyboard.Vanilla => Vanilla,
            Settings.VirtualKeyboard.Yes => Yes,
            Settings.VirtualKeyboard.No => No,
            _ => throw new Unreachable()
        };

    extension(Settings.VirtualKeyboard virtualKeyboard) {
        public bool isVanilla => virtualKeyboard == Settings.VirtualKeyboard.Vanilla;
        public bool isYes => virtualKeyboard == Settings.VirtualKeyboard.Yes;
        public bool isNo => virtualKeyboard == Settings.VirtualKeyboard.No;
    }

    public static TResult Match<TResult>(this Settings.Rumble rumble, TResult Default, TResult Vanilla, TResult Yes, TResult No)
        => rumble switch {
            Settings.Rumble.Default => Default,
            Settings.Rumble.Vanilla => Vanilla,
            Settings.Rumble.Yes => Yes,
            Settings.Rumble.No => No,
            _ => throw new Unreachable()
        };

    extension(Settings.Rumble rumble) {
        public bool isDefault => rumble == Settings.Rumble.Default;
        public bool isVanilla => rumble == Settings.Rumble.Vanilla;
        public bool isYes => rumble == Settings.Rumble.Yes;
        public bool isNo => rumble == Settings.Rumble.No;
    }

    public static TResult Match<TResult>(this Settings.MouseSensitivity mouseSensitivity, TResult Mouse, TResult Joystick)
        => mouseSensitivity switch {
            Settings.MouseSensitivity.Mouse => Mouse,
            Settings.MouseSensitivity.Joystick => Joystick,
            _ => throw new Unreachable()
        };

    extension(Settings.MouseSensitivity mouseSensitivity) {
        public bool isMouse => mouseSensitivity == Settings.MouseSensitivity.Mouse;
        public bool isJoystick => mouseSensitivity == Settings.MouseSensitivity.Joystick;
    }

    public static TResult Match<TResult>(this Settings.RollAxis rollAxis, TResult Yaw, TResult X, TResult Y)
        => rollAxis switch {
            Settings.RollAxis.Yaw => Yaw,
            Settings.RollAxis.X => X,
            Settings.RollAxis.Y => Y,
            _ => throw new Unreachable()
        };

    extension(Settings.RollAxis rollAxis) {
        public bool isYaw => rollAxis == Settings.RollAxis.Yaw;
        public bool isX => rollAxis == Settings.RollAxis.X;
        public bool isY => rollAxis == Settings.RollAxis.Y;
    }

    public static TResult Match<TResult>(this Settings.GyroSpace gyroSpace, TResult LocalYaw, TResult LocalRoll, TResult PlayerTurn, TResult PlayerLean)
        => gyroSpace switch {
            Settings.GyroSpace.LocalYaw => LocalYaw,
            Settings.GyroSpace.LocalRoll => LocalRoll,
            Settings.GyroSpace.PlayerTurn => PlayerTurn,
            Settings.GyroSpace.PlayerLean => PlayerLean,
            _ => throw new Unreachable()
        };

    extension(Settings.GyroSpace gyroSpace) {
        public bool isLocalYaw => gyroSpace == Settings.GyroSpace.LocalYaw;
        public bool isLocalRoll => gyroSpace == Settings.GyroSpace.LocalRoll;
        public bool isPlayerTurn => gyroSpace == Settings.GyroSpace.PlayerTurn;
        public bool isPlayerLean => gyroSpace == Settings.GyroSpace.PlayerLean;
    }

    public static TResult Match<TResult>(this Settings.GyroButtonMode gyroButtonMode, TResult Disabled, TResult Hold, TResult Mute, TResult Toggle)
        => gyroButtonMode switch {
            Settings.GyroButtonMode.Disabled => Disabled,
            Settings.GyroButtonMode.Hold => Hold,
            Settings.GyroButtonMode.Mute => Mute,
            Settings.GyroButtonMode.Toggle => Toggle,
            _ => throw new Unreachable()
        };

    extension(Settings.GyroButtonMode gyroButtonMode) {
        public bool isDisabled => gyroButtonMode == Settings.GyroButtonMode.Disabled;
        public bool isHold => gyroButtonMode == Settings.GyroButtonMode.Hold;
        public bool isMute => gyroButtonMode == Settings.GyroButtonMode.Mute;
        public bool isToggle => gyroButtonMode == Settings.GyroButtonMode.Toggle;
    }

    public static TResult Match<TResult>(this Settings.GyroCalibrateButtonMode gyroCalibrateButtonMode, TResult Menu, TResult Press, TResult Hold)
        => gyroCalibrateButtonMode switch {
            Settings.GyroCalibrateButtonMode.Menu => Menu,
            Settings.GyroCalibrateButtonMode.Press => Press,
            Settings.GyroCalibrateButtonMode.Hold => Hold,
            _ => throw new Unreachable()
        };

    extension(Settings.GyroCalibrateButtonMode gyroCalibrateButtonMode) {
        public bool isMenu => gyroCalibrateButtonMode == Settings.GyroCalibrateButtonMode.Menu;
        public bool isPress => gyroCalibrateButtonMode == Settings.GyroCalibrateButtonMode.Press;
        public bool isHold => gyroCalibrateButtonMode == Settings.GyroCalibrateButtonMode.Hold;
    }

    public static TResult Match<TResult>(this Settings.FlickSnapping flickSnapping, TResult None, TResult One, TResult Two, TResult Four, TResult Six, TResult Eight)
        => flickSnapping switch {
            Settings.FlickSnapping.None => None,
            Settings.FlickSnapping.One => One,
            Settings.FlickSnapping.Two => Two,
            Settings.FlickSnapping.Four => Four,
            Settings.FlickSnapping.Six => Six,
            Settings.FlickSnapping.Eight => Eight,
            _ => throw new Unreachable()
        };

    extension(Settings.FlickSnapping flickSnapping) {
        public bool isNone => flickSnapping == Settings.FlickSnapping.None;
        public bool isOne => flickSnapping == Settings.FlickSnapping.One;
        public bool isTwo => flickSnapping == Settings.FlickSnapping.Two;
        public bool isFour => flickSnapping == Settings.FlickSnapping.Four;
        public bool isSix => flickSnapping == Settings.FlickSnapping.Six;
        public bool isEight => flickSnapping == Settings.FlickSnapping.Eight;
    }

    public static TResult Match<TResult>(this Settings.ProbeOverlaps probeOverlaps, TResult Primary, TResult Both)
        => probeOverlaps switch {
            Settings.ProbeOverlaps.Primary => Primary,
            Settings.ProbeOverlaps.Both => Both,
            _ => throw new Unreachable()
        };

    extension(Settings.ProbeOverlaps probeOverlaps) {
        public bool isPrimary => probeOverlaps == Settings.ProbeOverlaps.Primary;
        public bool isBoth => probeOverlaps == Settings.ProbeOverlaps.Both;
    }

    public static TResult Match<TResult>(this Settings.ProbeLauncher probeLauncher, TResult Default, TResult Vanilla, TResult Hybrid, TResult Gamepad, TResult KBM)
        => probeLauncher switch {
            Settings.ProbeLauncher.Default => Default,
            Settings.ProbeLauncher.Vanilla => Vanilla,
            Settings.ProbeLauncher.Hybrid => Hybrid,
            Settings.ProbeLauncher.Gamepad => Gamepad,
            Settings.ProbeLauncher.KBM => KBM,
            _ => throw new Unreachable()
        };

    extension(Settings.ProbeLauncher probeLauncher) {
        public bool isDefault => probeLauncher == Settings.ProbeLauncher.Default;
        public bool isVanilla => probeLauncher == Settings.ProbeLauncher.Vanilla;
        public bool isHybrid => probeLauncher == Settings.ProbeLauncher.Hybrid;
        public bool isGamepad => probeLauncher == Settings.ProbeLauncher.Gamepad;
        public bool isKBM => probeLauncher == Settings.ProbeLauncher.KBM;
    }

    public static TResult Match<TResult>(this Settings.Autopilot autopilot, TResult Default, TResult Vanilla, TResult Gamepad, TResult KBM)
        => autopilot switch {
            Settings.Autopilot.Default => Default,
            Settings.Autopilot.Vanilla => Vanilla,
            Settings.Autopilot.Gamepad => Gamepad,
            Settings.Autopilot.KBM => KBM,
            _ => throw new Unreachable()
        };

    extension(Settings.Autopilot autopilot) {
        public bool isDefault => autopilot == Settings.Autopilot.Default;
        public bool isVanilla => autopilot == Settings.Autopilot.Vanilla;
        public bool isGamepad => autopilot == Settings.Autopilot.Gamepad;
        public bool isKBM => autopilot == Settings.Autopilot.KBM;
    }

    public static TResult Match<TResult>(this Settings.ShipLog shipLog, TResult Default, TResult Vanilla, TResult Gamepad, TResult KBM)
        => shipLog switch {
            Settings.ShipLog.Default => Default,
            Settings.ShipLog.Vanilla => Vanilla,
            Settings.ShipLog.Gamepad => Gamepad,
            Settings.ShipLog.KBM => KBM,
            _ => throw new Unreachable()
        };

    extension(Settings.ShipLog shipLog) {
        public bool isDefault => shipLog == Settings.ShipLog.Default;
        public bool isVanilla => shipLog == Settings.ShipLog.Vanilla;
        public bool isGamepad => shipLog == Settings.ShipLog.Gamepad;
        public bool isKBM => shipLog == Settings.ShipLog.KBM;
    }

    public static TResult Match<TResult>(this Settings.RoastingStick roastingStick, TResult Default, TResult Vanilla, TResult Gamepad, TResult KBM)
        => roastingStick switch {
            Settings.RoastingStick.Default => Default,
            Settings.RoastingStick.Vanilla => Vanilla,
            Settings.RoastingStick.Gamepad => Gamepad,
            Settings.RoastingStick.KBM => KBM,
            _ => throw new Unreachable()
        };

    extension(Settings.RoastingStick roastingStick) {
        public bool isDefault => roastingStick == Settings.RoastingStick.Default;
        public bool isVanilla => roastingStick == Settings.RoastingStick.Vanilla;
        public bool isGamepad => roastingStick == Settings.RoastingStick.Gamepad;
        public bool isKBM => roastingStick == Settings.RoastingStick.KBM;
    }

}
