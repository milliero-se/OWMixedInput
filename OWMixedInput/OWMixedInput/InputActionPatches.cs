using HarmonyLib;
using System.Reflection;

using UnityEngine;
using UnityEngine.InputSystem;

namespace OWMixedInput;

// Patches and extensions for the IInputAction hierarchy.
//
// We choose not to use transpiler methods here so that the code is easier to read.
// Even if we did, there is no hope for compatibility between this mod and another
// which also modifies the IInputAction hierarchy.
[HarmonyPatch]
public static class InputActionsPatches {
    // Rumble functionality needs to access gamepad identifiers, even when the UI setting is KBM.
    public static AxisIdentifier GamepadAxisID(this IInputAction action)
        => ((AbstractInputAction)action)._axisIdGamepad;

    // The IInputAction hierarchy exposes events, which map the the events in Unity's input system.
    // The vanilla only barely uses these events. For the sake of this mod, they are considered deprecated.
    //
    // Thus, we patch the EnableAction method to avoid subscribing to Unity's events.
    [HarmonyPrefix, HarmonyPatch(typeof(AbstractInputAction), nameof(AbstractInputAction.EnableAction))]
    static bool AbstractInputAction_EnableAction_Patch(InputAction action, bool enable) {
        if (action == null) {
            return false;
        }
        if (enable) {
            action.Enable();
            //// Vanilla
            //action.started += InputStarted;
            //action.performed += InputPerformed;
            //action.canceled += InputCancelled;
        } else {
            //// Vanilla
            //action.started -= InputStarted;
            //action.performed -= InputPerformed;
            //action.canceled -= InputCancelled;
            action.Disable();
        }
        return false;
    }

    // The IInputAction hierarchy exposes the Unity input system's Phase information.
    // We will not use that part of the input system in this mod, so we make the Phase field,
    // the PhaseUpdate method, and the event methods into errors.
    //
    // The vanilla mouse processing method is unsupported, as we use our own implementation of
    // this behavior. Thus, we make the TryApplyMouseProcessing method into an error.
    //
    // The HasActiveMouseInput field is unsupported, as the game should not care whether
    // or not a mouse is involved in an input. We make this field an error.
    [HarmonyPrefix]
    [HarmonyPatch(typeof(AbstractInputAction), nameof(AbstractInputAction.Phase), MethodType.Getter)]
    [HarmonyPatch(typeof(AbstractInputAction), nameof(AbstractInputAction.Phase), MethodType.Setter)]
    [HarmonyPatch(typeof(AbstractInputAction), nameof(AbstractInputAction.PhaseUpdate))]
    [HarmonyPatch(typeof(InputActionPair), nameof(InputActionPair.PhaseUpdate))]
    [HarmonyPatch(typeof(AbstractInputAction), nameof(AbstractInputAction.InputStarted))]
    [HarmonyPatch(typeof(AbstractInputAction), nameof(AbstractInputAction.InputPerformed))]
    [HarmonyPatch(typeof(AbstractInputAction), nameof(AbstractInputAction.InputCancelled))]
    [HarmonyPatch(typeof(AbstractInputAction), nameof(AbstractInputAction.TryApplyMouseProcessing))]
    [HarmonyPatch(typeof(AbstractInputAction), nameof(AbstractInputAction.HasActiveMouseInput), MethodType.Getter)]
    [HarmonyPatch(typeof(AbstractInputAction), nameof(AbstractInputAction.HasActiveMouseInput), MethodType.Setter)]
    static void UnsupportedMethod(MethodBase __originalMethod) {
        throw new Exception(__originalMethod.ToString());
    }

    // The SetAction methods of the IInputAction hierarchy need to be modified.
    // We mostly keep the vanilla behavior, with a few changes:
    // We initialize the ValueType of an action once instead of during Update.
    // We initialize the axisSmoothingBuffer used for mouse inputs instead of checking each Update.
    // We avoid usage of the _mouseAxisControl fields because we don't care about them.
    // The InputActionPair method also calls to MixedInputUtils.MaskOverlappingBindings.
    [HarmonyPrefix, HarmonyPatch(typeof(BasicInputAction), nameof(BasicInputAction.SetAction))]
    static bool BasicInputAction_SetAction_Patch(BasicInputAction __instance, InputAction action) {
        if (action == null) {
            Debug.LogError("Failed to set InputActions for InputAction " + action.name + ". Expected a Secondary Action.");
            return false;
        }
        __instance.Enable(enable: false);
        __instance.Action = action;
        __instance.ValueType = InputConsts.FindValueType(__instance.Action);
        __instance.InitializeAxisID();
        //// Vanilla
        //_mouseAxisControl = null;
        //for (int i = 0; i < Action.controls.Count; i++)
        //{
        //    if (Action.controls[i].device is Mouse && !(Action.controls[i] is ButtonControl))
        //    {
        //        _mouseAxisControl = Action.controls[i];
        //        break;
        //    }
        //}

        // Reset the smoothing buffer
        // Now we don't need to do this in the update loop.
        __instance._smoothingBuffer = new AxisSmoothingBuffer();

        __instance.Enable(enable: true);

        return false;
    }
    [HarmonyPrefix, HarmonyPatch(typeof(AxisInputAction), nameof(AxisInputAction.SetAction))]
    static bool AxisInputAction_SetAction_Patch(AxisInputAction __instance, InputAction action) {
        if (action == null) {
            Debug.LogError("Failed to set InputAction..");
            return false;
        }
        __instance.Enable(enable: false);
        __instance.Action = action;

        // Set the ValueType here so that we do not need to do so in the update loop.
        __instance.ValueType = InputConsts.FindValueType(__instance.Action);

        __instance.InitializeAxisID();
        //// Vanilla
        //_mouseAxisControl = null;
        //for (int i = 0; i < Action.controls.Count; i++)
        //{
        //    if (Action.controls[i].device is Mouse && !(Action.controls[i] is ButtonControl))
        //    {
        //        _mouseAxisControl = Action.controls[i];
        //        break;
        //    }
        //}

        // Reset the mouse smoothing buffer.
        // Now we don't need to do this in the update loop.
        __instance._smoothingBuffer = new AxisSmoothingBuffer();

        __instance.Enable(enable: true);

        return false;
    }
    [HarmonyPrefix, HarmonyPatch(typeof(InputActionPair), nameof(InputActionPair.SetAction))]
    static bool InputActionPair_SetAction_Patch(InputActionPair __instance, InputAction primary, InputAction secondary) {
        if (primary == null || secondary == null) {
            Debug.LogError("Failed to set InputActions for InputActionPair.");
            return false;
        }
        __instance.Enable(enable: false);
        __instance.PrimaryAction = primary;
        __instance.SecondaryAction = secondary;

        // Set the ValueType here so that we do not need to do so in the update loop.
        __instance.ValueType = InputConsts.InputValueType.SINGLE_AXIS;

        //// Vanilla
        //PostInitializeActions();

        // We do our own post initialize actions.
        {
            __instance.InitializeAxisID();

            // Unlike vanilla, we mask out the primary bindings from the secondary action
            // so that we do not need to check for overlapping bindings every frame.
            __instance.SecondaryAction.MaskOverlappingBindings(__instance.PrimaryAction);
        }

        // Reset the primary mouse smoothing buffer.
        // We only need one. The secondary buffer is not used.
        __instance._smoothingBufferPrimary = new AxisSmoothingBuffer();

        __instance.Enable(enable: true);

        return false;
    }

    // The DoUpdate methods of the IInputAction hierarchy are changed substantially.
    // We use the MixedInputUtils MixedInputAction types to unify the input implementations
    // across the hierarchy.
    //
    // The main difference in behavior is that now the Value field of the action only stores
    // the "gamepad" inputs (i.e. non-mouse), and instead the AxisSmoothingBuffer is responsible
    // for holding the mouse inputs.
    //
    // Note that mouse smoothing is still disabled according to the user setting; the smoothing
    // buffer still holds the mouse input regardless of whether or not smoothing is applied.
    //
    // We also need to call the OWMixedInput.UpdateFromAction method as our replacement for the
    // Unity input system event callbacks.
    [HarmonyPrefix, HarmonyPatch(typeof(BasicInputAction), nameof(BasicInputAction.DoUpdate))]
    static bool BasicInputAction_DoUpdate_Patch(BasicInputAction __instance) {
        var contributions = __instance.MixedAction().Read();

        // Mouse input is only supported in a single axis because we only have one smoothing buffer.
        if (contributions.mouse.y != 0f) {
            throw new Exception("Vector mouse input in BasicInputAction!");
        }

        __instance._smoothingBuffer.Update(contributions.mouse.x);
        __instance.Value = contributions.gamepad;

        // Inform the MixedInputManager of our update.
        OWMixedInput.UpdateFromAction(__instance.Action);

        return false;
    }
    [HarmonyPrefix, HarmonyPatch(typeof(AxisInputAction), nameof(AxisInputAction.DoUpdate))]
    static bool AxisInputAction_DoUpdate_Patch(AxisInputAction __instance) {
        var contributions = __instance.MixedAction().Read();

        __instance._smoothingBuffer.Update(contributions.mouse);
        __instance.Value = contributions.gamepad;

        // Inform the MixedInputManager of our update.
        OWMixedInput.UpdateFromAction(__instance.Action);

        return false;
    }
    [HarmonyPrefix, HarmonyPatch(typeof(InputActionPair), nameof(InputActionPair.DoUpdate))]
    static bool InputActionPair_DoUpdate_Patch(InputActionPair __instance) {
        var contributions = __instance.MixedAction().Read();

        __instance._smoothingBufferPrimary.Update(contributions.mouse);
        __instance.Value = contributions.gamepad;

        // Inform the MixedInputManager of our update.
        OWMixedInput.UpdateFromAction(__instance.SecondaryAction);
        OWMixedInput.UpdateFromAction(__instance.PrimaryAction);

        return false;
    }

    // Extension methods to get mouse inputs from an IInputActions.
    // As discussed above, the mouse values are stored exclusively in the AxisSmoothingBuffers.
    extension(BasicInputAction action) {
        public Vector2 MouseValue => new Vector2(action._smoothingBuffer.TryGetAverage() * MixedInputUtils.MOUSE_SCALE, 0f);
    }
    extension(IVectorInputAction action) {
        public Vector2 MouseValue
            => action switch {
                BasicInputAction basic => basic.MouseValue,
                _ => throw new Exception("Unsupported IVectorInputAction type")
            };
    }
    extension(AxisInputAction action) {
        public float MouseValue => action._smoothingBuffer.TryGetAverage() * MixedInputUtils.MOUSE_SCALE;
    }
    extension(InputActionPair action) {
        public float MouseValue => action._smoothingBufferPrimary.TryGetAverage() * MixedInputUtils.MOUSE_SCALE;
    }
    extension(IAxisInputAction action) {
        public float MouseValue
            => action switch {
                AxisInputAction axis => axis.MouseValue,
                InputActionPair pair => pair.MouseValue,
                _ => throw new Exception("Unsupported IAxisInputAction type")
            };
    }
}
