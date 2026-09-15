using OWML.Common;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace OWMixedInput;

// Common utilities for input processing.
public static class MixedInputUtils {
    // The vanilla game has separate control paths for mouse and non-mouse inputs.
    // We bundle these two types of inputs into a single struct, Contributions.
    // All non-mouse inputs will be considered "gamepad" inputs for the purposes
    // of this struct given that they are all processed the same.
    //
    // Because C# generics suck, we have to write essentially the same struct twice.
    public record Contributions {
        public float mouse;
        public float gamepad;

        public static Contributions Mouse(float mouse)
            => new Contributions { mouse = mouse, gamepad = 0f };
        public static Contributions Gamepad(float gamepad)
            => new Contributions { mouse = 0f, gamepad = gamepad };

        public static Contributions zero
            => new Contributions { mouse = 0f, gamepad = 0f };

        public static Contributions operator +(Contributions lhs, Contributions rhs)
            => new Contributions {
                mouse = lhs.mouse + rhs.mouse,
                gamepad = lhs.gamepad + rhs.gamepad
            };

        public static Contributions operator -(Contributions lhs, Contributions rhs)
            => new Contributions {
                mouse = lhs.mouse - rhs.mouse,
                gamepad = lhs.gamepad - rhs.gamepad
            };
    }

    public record Contributions2 {
        public Vector2 mouse;
        public Vector2 gamepad;

        public static Contributions2 Mouse(Vector2 mouse)
            => new Contributions2 { mouse = mouse, gamepad = Vector2.zero };
        public static Contributions2 Gamepad(Vector2 gamepad)
            => new Contributions2 { mouse = Vector2.zero, gamepad = gamepad };

        public static Contributions2 Axes(Contributions x, Contributions y)
            => new Contributions2 {
                mouse = new Vector2(x.mouse, y.mouse),
                gamepad = new Vector2(x.gamepad, y.gamepad)
            };

        public static Contributions2 zero
            => new Contributions2 { mouse = Vector2.zero, gamepad = Vector2.zero };

        public static Contributions2 operator +(Contributions2 lhs, Contributions2 rhs)
            => new Contributions2 {
                mouse = lhs.mouse + rhs.mouse,
                gamepad = lhs.gamepad + rhs.gamepad
            };

        public static Contributions2 operator -(Contributions2 lhs, Contributions2 rhs)
            => new Contributions2 {
                mouse = lhs.mouse - rhs.mouse,
                gamepad = lhs.gamepad - rhs.gamepad
            };
    }

    // IInputAction + PlayerCameraController provides a conversion rate between mouse and joystick inputs:
    // Gamepad * deltaTime == (Mouse * 1/20) * 1/60
    public const float MOUSE_SCALE = 1f/1200f;

    // Deadzone a magnitude (non-neg float) using an inverse lerp.
    public static float DeadzonedMagnitude(float magnitude) {
        var inner = 0.2f * OWInputProcessorUtil.InnerDeadZoneMultiplier;
        var outer = 0.05f * OWInputProcessorUtil.OuterDeadZoneMultiplier;

        return Mathf.InverseLerp(inner, 1f - outer, magnitude);
    }

    // Deadzone a vector.
    public static Vector2 Deadzone(Vector2 value)
        => value.normalized * DeadzonedMagnitude(value.magnitude);

    // Deadzone a float using its parent vector's magnitude.
    public static float Deadzone(float value, Vector2 parent) {
        var parentMagnitude = parent.magnitude;
        var correctedMagnitude = DeadzonedMagnitude(parentMagnitude);

        // parentMagnitude == 0 implies correctedMagnitude == 0;
        if (correctedMagnitude == 0) {
            return 0;
        } else {
            return value / parentMagnitude * correctedMagnitude;
        }
    }

    // Read Contributions from an AxisControl.
    public static Contributions ReadContribution(this AxisControl control) {
        float value = control.ReadUnprocessedValue();
        if (control.parent is StickControl parent) {
            // This stick input needs to get deadzoned.
            Vector2 parentValue = parent.ReadUnprocessedValue();

            value = Deadzone(value, parentValue);

            return Contributions.Gamepad(value);
        } else if (control is ButtonControl) {
            // Button control includes mouse buttons, trigger axes, and actual buttons.
            return Contributions.Gamepad(value);
        } else if (control.device is Mouse) {
            // This is a mouse axis
            return Contributions.Mouse(value);
        } else {
            // Anything else might as well be gamepad.
            OWMixedInput.WriteLine($"Unknown control type: {control.path}", MessageType.Warning);
            return Contributions.Gamepad(value);
        }
    }

    // Read Contributions2 from a Vector2Control.
    public static Contributions2 ReadContribution(this Vector2Control control) {
        Vector2 value = control.ReadUnprocessedValue();
        if (control is StickControl) {
             // This stick input needs to get deadzoned.
             value = Deadzone(value);

             return Contributions2.Gamepad(value);
        } else if (control.device is Mouse) {
            return Contributions2.Mouse(value);
        } else {
            // Anything else might as well be a gamepad.
            OWMixedInput.WriteLine($"Unknown control type: {control.path}", MessageType.Warning);
            return Contributions2.Gamepad(value);
        }
    }

    // MixedInputActions will mirror the IInputAction hierarchy.
    // The struct will wrap Unity InputAction(s) and provide methods to read processed values.

    // For BasicInputAction
    public struct BasicMixedInputAction {
        public InputAction action;

        static Contributions2 ReadControl(InputControl control)
            => control switch {
                AxisControl axis =>
                    Contributions2.Axes(axis.ReadContribution(), Contributions.zero),
                Vector2Control vector =>
                    vector.ReadContribution(),
                _ =>
                    throw new Exception($"Unsupported InputControl type in BasicMixedInputAction: {control.GetType()}")
            };

        public Contributions2 Read() {
            // Don't use LINQ in the Update loop
            var result = Contributions2.zero;
            var controls = action.controls;
            for (int i = 0; i < controls.Count; i++) {
                result += ReadControl(controls[i]);
            }
            return result;
        }
    }
    // For AxisInputAction
    public struct AxisMixedInputAction {
        public InputAction action;

        static Contributions ReadControl(InputControl control)
            => control switch {
                AxisControl axis =>
                    axis.ReadContribution(),
                _ =>
                    throw new Exception($"Unsupported InputControl type in AxisMixedInputAction: {control.GetType()}")
            };

        public Contributions Read() {
            // Don't use LINQ in the Update loop
            var result = Contributions.zero;
            var controls = action.controls;
            for (int i = 0; i < controls.Count; i++) {
                result += ReadControl(controls[i]);
            }
            return result;
        }
    }
    // For InputActionPair
    public struct DifferenceMixedInputAction {
        public AxisMixedInputAction positive;
        public AxisMixedInputAction negative;

        public Contributions Read()
            => positive.Read() - negative.Read();
    }

    // Methods for getting a MixedInputAction from an IInputAction.
    public static BasicMixedInputAction MixedAction(this BasicInputAction action)
        => new BasicMixedInputAction { action = action.Action };
    public static AxisMixedInputAction MixedAction(this AxisInputAction action)
        => new AxisMixedInputAction { action = action.Action };
    public static DifferenceMixedInputAction MixedAction(this InputActionPair action)
        => new DifferenceMixedInputAction {
            positive = new AxisMixedInputAction { action = action.PrimaryAction },
            negative = new AxisMixedInputAction { action = action.SecondaryAction }
        };

    // In vanilla code, InputActionPair represents a pair of actions that are subtracted to get a result
    // e.g. stick up & stick down or w & s. Often, a single action is bound twice e.g. mouse y & mouse y.
    // If the same action is both parts of the pair, the value should not be subtracted (it would be zero).
    //
    // The vanilla game implements this using UsingGamepad() and InputActionUtil.HasSameBinding which cannot
    // be correct for a bunch of inputs added together. Instead, we use Unity's bindingMask functionality.
    //
    // The MaskOverlappingBindings method hides overlapping inputs from a control.
    // Bindings on self which match other are masked off of self.
    public static void MaskOverlappingBindings(this InputAction self, InputAction other) {
        // Make sure there is not currently a mask.
        if (self.bindingMask != null) {
            throw new Exception("Refusing to overwrite non-null bindingMask");
        }

        var nonOverlappingPaths = self.bindings
            .Select((binding) => binding.path)
            .Where((path) => {
                var mask = new UnityEngine.InputSystem.InputBinding(path);
                return !other.bindings.Any((binding) => mask.Matches(binding));
            });
        var mask = string.Join(";", nonOverlappingPaths);
        self.bindingMask = new UnityEngine.InputSystem.InputBinding(mask);
    }

    // Get the average out of a smoothing buffer, or the latest value if the users has disabled
    // mouse smoothing.
    public static float TryGetAverage(this AxisSmoothingBuffer buffer)
        => OWMixedInput.Settings.mouseSmoothing
                ? buffer.GetAverage()
                : buffer._buffer[buffer._bufferIndex];

    // Get a non-NaN float or null
    extension(float value) {
        public float? NonNaN => float.IsNaN(value) ? null : value;
    }

    // SettingsSave computes two different sensitivities based on UsingGamepad.
    // In this mod, we only use the gamepad sensitivities, but we compute mouse
    // sensitivites from the gamepad sensitivities.
    //
    // Compute mouse sensitivity value from gamepad sensitivity value.
    public static float MouseSensitivity(float gamepadSensitivity) {
        if (OWMixedInput.Settings.mouseSensitivity.isJoystick) {
            return gamepadSensitivity;
        }

        // Sensitivities are computed via interpolating between the min and max setting.
        // The interpolation depends only on the user setting, so we compute the interpolation
        // amount and apply it to the other scale.
        var gamma = (gamepadSensitivity - SettingsSave._sensitivityMin) 
            / (SettingsSave._sensitivityMax - SettingsSave._sensitivityMin);

        return SettingsSave._sensitivityMinMouse + 
            gamma * (SettingsSave._sensitivityMaxMouse - SettingsSave._sensitivityMinMouse);
    }

    public const float DegToRad = Mathf.PI / 180f;
    public const float RadToDeg = 180f / Mathf.PI;

    public static Vector2 ToUnity(this System.Numerics.Vector2 v) => new Vector2(v.X, v.Y);
    public static Vector3 ToUnity(this System.Numerics.Vector3 v) => new Vector3(v.X, v.Y, v.Z);

    public static System.Numerics.Vector2 ToSystem(this Vector2 v) => new System.Numerics.Vector2(v.x, v.y);
    public static System.Numerics.Vector3 ToSystem(this Vector3 v) => new System.Numerics.Vector3(v.x, v.y, v.z);

    public static void ClampMagnitude(ref float f, float max = 1f) {
        var r = Mathf.Abs(f);

        if (r > max) {
            f *= (max / r);
        }
    }
    public static float ClampMagnitude(float f, float max = 1f) {
        ClampMagnitude(ref f, max);
        return f;
    }
    public static void ClampMagnitude(ref Vector2 v, float max = 1f) {
        var r = v.magnitude;

        if (r > max) {
            v *= (max / r);
        }
    }
    public static Vector2 ClampMagnitude(Vector2 v, float max = 1f) {
        ClampMagnitude(ref v, max);
        return v;
    }

    const float OriginEps = 1e-4f;
    const float CircleEps = 1e-4f;

    // Takes an input on the unit disk and produces an output on the [-1,1]^2 square.
    // The intention is that the mapping is "nice".
    //
    // Mathematically, this is the formula
    //  v |-> r / |v|_p(r) * v
    //      where r=|v| is the 2-norm/magnitude,
    //          |v|_p is the p-norm (|x|^p+|y|^p)^(1/p) for 1 <= p < \infty
    //          |v|_\infty = max(|x|,|y|)
    //          p(r) = 2/(1-r) if 0 <= r < 1 and p(r)=\infty otherwise.
    //
    // This function maps circles around the origin to p-norm "super"circles of the same radius.
    // Supercircles are also known as Lam\'e circles.
    // It becomes the identitity as |v| -> 0.
    //
    // This implementation is possibly over-complicated, I tried to mind precision problems.
    public static void Squareify(ref Vector2 v) {

        var r2 = v.sqrMagnitude;

        // p(r) -> 2 as r -> 0 so formula is identity near 0.
        if (r2 < OriginEps*OriginEps) {
            return;
        }

        // compute min and max absolute component
        // M is the \infty norm
        var M = Mathf.Abs(v.y);
        var m = Mathf.Abs(v.x);
        if (m > M) {
            (M, m) = (m, M);
        }

        var r = Mathf.Sqrt(r2);

        // p(r) -> \infty as r -> 1, so use \infty norm near 1.
        if (1 - r < CircleEps) {
            v *= r / M;
            return;
        }

        var p = 2/(1-r);

        // The quotient min / max. This lets us factor m from the p-norm.
        var q = m / M;

        // |v|_p(r)
        var n = M * Mathf.Pow(1+Mathf.Pow(q, p), 1/p);

        v *= r / n;
    }
    public static Vector2 Squareify(Vector2 v) {
        Squareify(ref v);
        return v;
    }
}

