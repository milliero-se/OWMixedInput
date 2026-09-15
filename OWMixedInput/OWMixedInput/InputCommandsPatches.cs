using HarmonyLib;
using System.Reflection;
using System.Reflection.Emit;

using OWML.Common;
using UnityEngine;

namespace OWMixedInput;

// Patches and extensions for the IInputCommands hierarchy.
//
// We choose not to use transpiler methods here so that the code is easier to read.
// Even if we did, there is no hope for compatibility between this mod and another
// which also modifies the IInputCommands hierarchy.
[HarmonyPatch]
public static class InputCommandsPatches {
    // Rumble functionality needs to access gamepad identifiers, even when the UI setting is KBM.
    // This method does not support CompositeInputCommands as rumble does not need it.
    public static AxisIdentifier GamepadAxisID(this IInputCommands command)
        => command switch {
            ISingleInputCommand singleCommand => singleCommand.Action.GamepadAxisID(),
            _ => throw new Exception($"Unsupported IInputCommands type in GamepadAxisID: {command.GetType()}")
        };

    // The accumulation behavior of the IInputCommands hierarchy is entirely revamped.
    // The AxisValue field of an IInputCommands is now only responsible for mouse input.
    // The non-mouse inputs are stored in the underlying IInputAction types.
    // Therefore, axis values will always accumulate.
    //
    // The ShouldAccumulate field is unsupported and is now an error.
    //
    // The ActiveDevice method cannot be used for good, so it is also now an error.
    [HarmonyPrefix]
    [HarmonyPatch(typeof(AbstractCommands), nameof(AbstractCommands.ShouldAccumulate), MethodType.Getter)]
    [HarmonyPatch(typeof(AbstractCommands), nameof(AbstractCommands.ShouldAccumulate), MethodType.Setter)]
    [HarmonyPatch(typeof(InputCommands), nameof(InputCommands.GetActiveDevice))]
    [HarmonyPatch(typeof(InputAxisCommands), nameof(InputAxisCommands.GetActiveDevice))]
    [HarmonyPatch(typeof(CompositeInputCommands), nameof(CompositeInputCommands.GetActiveDevice))]
    static void UnsupportedMethod(MethodBase __originalMethod) {
        throw new Exception(__originalMethod.ToString());
    }

    // We distinguish between a sensitivity of 1 and an unset sensitivity.
    // Therefore, we set the sensitivity to NaN in the IInputCommands constructors.
    // For some reason, using one declaration for this does not work.
    [HarmonyPostfix, HarmonyPatch(typeof(InputCommands), MethodType.Constructor, 
            [typeof(InputConsts.InputCommandType), typeof(IVectorInputAction)])]
    static void InputCommands_Constructor_Postfix(InputCommands __instance) {
        __instance.Sensitivity = float.NaN;
    }
    [HarmonyPostfix, HarmonyPatch(typeof(InputAxisCommands), MethodType.Constructor, 
            [typeof(InputConsts.InputCommandType), typeof(IAxisInputAction)])]
    static void InputAxisCommands_Constructor_Postfix(InputAxisCommands __instance) {
        __instance.Sensitivity = float.NaN;
    }
    [HarmonyPostfix, HarmonyPatch(typeof(CompositeInputCommands), MethodType.Constructor, 
            [typeof(InputConsts.InputCommandType), typeof(IInputActionPair), typeof(IInputActionPair)])]
    static void CompositeInputCommands_Constructor_Postfix(CompositeInputCommands __instance) {
        __instance.Sensitivity = float.NaN;
    }

    // AbstractCommands.Update method is surprisingly mostly correct.
    // The only adjustment we make is for updates to accumulate when time is paused.
    [HarmonyPrefix]
    [HarmonyPatch(typeof(AbstractCommands), nameof(AbstractCommands.Update))]
    static bool AbstractCommands_Update_Patch(AbstractCommands __instance) {
        __instance.Consumed = false;
        __instance.WasActiveLastFrame = __instance.IsActiveThisFrame;
        __instance.UpdateFromAction();

        if (__instance._clearAccumulationFlag) {
            __instance.AxisValueAccumulated = new Vector2(0f, 0f);
            __instance._clearAccumulationFlag = false;
        }
        __instance.AxisValueAccumulated += __instance.AxisValue;
        //// Vanilla
        //if (OWTime.IsPaused())
        //{
        //    _clearAccumulationFlag = true;
        //}
        if (!__instance.IsActiveThisFrame) {
            if (__instance.WasActiveLastFrame) {
                __instance.InputStartedTime = float.MaxValue;
            }
        } else if (!__instance.WasActiveLastFrame) {
            __instance.InputStartedTime = Time.realtimeSinceStartup;
        }

        return false;
    }

    // Extension methods to get gamepad input from IInputCommands.
    // The gamepad input is stored in the underlying actions, not in the AxisValue field.
    extension(InputCommands command) {
        Vector2 RawGamepad => command._action.Value;
    }
    extension(InputAxisCommands command) {
        Vector2 RawGamepad => new Vector2(command._action.Value, 0f);
    }
    extension(CompositeInputCommands command) {
        Vector2 RawGamepad => new Vector2(command._secondaryAction.Value, command._primaryAction.Value);
    }
    extension(IInputCommands command) {
        Vector2 RawGamepad => command switch {
            InputCommands vector => vector.RawGamepad,
            InputAxisCommands axis => axis.RawGamepad,
            CompositeInputCommands composite => composite.RawGamepad,
            _ => throw new Exception($"Unsupported IInputCommands type: {command.GetType()}")
        };
    }
    
    // Extension methods to get mouse input from IInputCommands.
    // The mouse input is stored in the AxisValue and AxisValueAccumulated fields.
    extension(AbstractCommands command) {
        Vector2 RawMouse(bool asVelocity = false)
            => OWMixedInput.ProcessDirectInput(command.AxisValue, command.AxisValueAccumulated, asVelocity);
    }
    extension(IInputCommands command) {
        Vector2 RawMouse(bool asVelocity = false) => command switch {
            AbstractCommands abstractCommand => abstractCommand.RawMouse(asVelocity),
            _ => throw new Exception($"Unsupported IInputCommands type: {command.GetType()}")
        };
    }

    // Extension methods to get the AbstractCommands fields from an IInputCommands.
    // Since every IInputCommands is an AbstractCommands, these methods should not fail.
    extension(IInputCommands command) {
        Vector2 AxisValue => ((AbstractCommands)command).AxisValue;
        Vector2 AxisValueAccumulated => ((AbstractCommands)command).AxisValueAccumulated;

        Vector2 InversionFactorVector => ((AbstractCommands)command)._inversionFactorVector;
    }

    // Extension methods to get input values from IInputCommands with more granularity
    // than the vanilla methods. These will be used to implement the vanilla methods.
    extension(IInputCommands command) {
        // Get an AxisValue out of an IInputCommands.
        //
        // Gamepad and mouse inputs are obtained via the methods above.
        //
        // If asVelocity is true, the value should be interpreted as a velocity like the vanilla game does.
        // If includeMouse is true, the mouse inputs are divided by OWMixedInput.deltaTime to convert them
        // into a velocity.
        // If includeGamepad is true, the gamepad inputs are included as-is.
        //
        // If asVelocity is false, the value should be interpreted as a change in position, like a mouse input.
        // If includeMouse is true, the moust inputs are included as-is.
        // If includeGamepad is true, the gamepad inputs are premultiplied by Time.unscaledDeltaTime.
        //
        // If clamp is true, the returned value will have magnitude at most 1.
        public Vector2 GetAxisValue(
            bool useSensitivity = true,
            bool useInversionFactor = true,
            bool includeMouse = true,
            bool includeGamepad = true,
            bool asVelocity = true,
            bool clamp = true
        ) {
            var value = Vector2.zero;

            // command.Sensitivity uses NaN to indicate unset.
            // Convert to nullable float using NonNaN.
            float? sensitivity = useSensitivity ? command.Sensitivity.NonNaN : null;

            if (includeMouse) {
                var mouseSensitivity = sensitivity switch {
                    float s => MixedInputUtils.MouseSensitivity(s),
                    null => 1f
                };

                value += command.RawMouse(asVelocity) * mouseSensitivity;
            }

            if (includeGamepad) {
                var gamepadSensitivity = sensitivity ?? 1f;

                if (asVelocity) {
                    value += command.RawGamepad * gamepadSensitivity;
                } else {
                    value += command.RawGamepad * gamepadSensitivity * Time.unscaledDeltaTime;
                }
            }

            if (clamp && asVelocity) {
                MixedInputUtils.ClampMagnitude(ref value);
            }

            if (useInversionFactor) {
                value *= command.InversionFactorVector;
            }

            return value;
        }

        // Get an AxisValue as a float.
        // Unfortunately, this must be a separate method because the vanilla code
        // uses the y axis of the inversion factor and the x axis of the input value.
        //
        // When clamp is true, the returned value is clamped so that the Vector2 AxisValue
        // does not have magnitude greater than 1. Thus, this is not the same setting clamp 
        // to false and then clamping the returned float yourself.
        public float GetValue(
            bool useSensitivity = true,
            bool useInversionFactor = true,
            bool includeMouse = true,
            bool includeGamepad = true,
            bool asVelocity = true,
            bool clamp = true
        ) {
            var value = command.GetAxisValue(
                    useSensitivity: useSensitivity,
                    useInversionFactor: false,
                    includeMouse: includeMouse,
                    includeGamepad: includeGamepad,
                    asVelocity: asVelocity,
                    clamp: clamp
                ).x;

            if (useInversionFactor) {
                value *= command.InversionFactor;
            }

            return value;
        }
    }


    // Patch the vanilla GetAxisValue and GetValue methods to match the behavior above.
    // Code calling the vanilla methods will include mouse input and will be clamped.
    [HarmonyPrefix, HarmonyPatch(typeof(AbstractCommands), nameof(AbstractCommands.GetAxisValue))]
    static bool AbstractCommands_GetAxisValue_Patch(AbstractCommands __instance, bool useSensitivity, ref Vector2 __result) {
        __result = __instance.GetAxisValue(
                useSensitivity: useSensitivity,
                useInversionFactor: true,
                includeMouse: true,
                includeGamepad: true,
                asVelocity: true,
                clamp: true
            );
        return false;
    }
    [HarmonyPrefix, HarmonyPatch(typeof(AbstractCommands), nameof(AbstractCommands.GetValue))]
    static bool AbstractCommands_GetValue_Patch(AbstractCommands __instance, ref float __result) {
        __result = __instance.GetValue(
                useSensitivity: true,
                useInversionFactor: true,
                includeMouse: true,
                includeGamepad: true,
                asVelocity: true,
                clamp: true
            );
        return false;
    }

    // The AbstractInputCommands.UpdateFromAction is incorrect after our changes.
    // This is unfortunate because it is a generic method, and it is called from its derived classes.
    // Since the derived methods are wrong too, we disable this method and patch the derived implementations.
    //
    // As far as I can tell, there are four values of the generic parameter in the codebase:
    // IAxisInputAction, RebindableAxisInputAction, IVectorInputAction, and RebindableInputActionPair.
    [HarmonyPrefix]
    [HarmonyPatch(typeof(AbstractInputCommands<IAxisInputAction>), nameof(AbstractInputCommands<IAxisInputAction>.UpdateFromAction))]
    [HarmonyPatch(typeof(AbstractInputCommands<RebindableAxisInputAction>), nameof(AbstractInputCommands<RebindableAxisInputAction>.UpdateFromAction))]
    [HarmonyPatch(typeof(AbstractInputCommands<IVectorInputAction>), nameof(AbstractInputCommands<IVectorInputAction>.UpdateFromAction))]
    [HarmonyPatch(typeof(AbstractInputCommands<RebindableInputActionPair>), nameof(AbstractInputCommands<RebindableInputActionPair>.UpdateFromAction))]
    static void UnsupportedUpdateFromAction(MethodBase __originalMethod) {
        throw new Exception(__originalMethod.ToString());
    }

    // The UpdateFromAction methods need to be updated. All the special behavior of the vanilla methods is now gone,
    // and the three implementations of this method all look more-or-less identical.
    //
    // The changes are as follows:
    // The AxisValue field is computed from the mouse inputs of the underlying action, as discussed above.
    // All special casing for mouse input and deadzoning has been removed. The IInputAction hierarchy handles
    // correct deadzoning of composite stick controls by itself.
    //
    // The IsActiveThisFrame behavior is replaced with a simple check that the axis value returned by
    // GetAxisValue has magnitude above the commands's PressedThreshold. No Phase information is involved.
    [HarmonyPrefix, HarmonyPatch(typeof(InputCommands), nameof(InputCommands.UpdateFromAction))]
    static bool InputCommands_UpdateFromAction_Patch(InputCommands __instance) {
        __instance.Action.Update();
        __instance.AxisValue = __instance._action.MouseValue;

        __instance.AxisID = __instance.Action.AxisID;

        var value = __instance
            .GetAxisValue(
                useSensitivity: false,
                useInversionFactor: false,
                includeMouse: true,
                includeGamepad: true,
                asVelocity: true,
                clamp: false
            ).magnitude;

        __instance.IsActiveThisFrame = value > __instance.PressedThreshold;

        return false;
    }
    [HarmonyPrefix, HarmonyPatch(typeof(InputAxisCommands), nameof(InputAxisCommands.UpdateFromAction))]
    static bool InputAxisCommands_UpdateFromAction_Patch(InputAxisCommands __instance) {
        __instance.Action.Update();
        __instance.AxisValue = new Vector2(__instance._action.MouseValue, 0f);

        __instance.AxisID = __instance.Action.AxisID;

        var value = __instance
            .GetAxisValue(
                useSensitivity: false,
                useInversionFactor: false,
                includeMouse: true,
                includeGamepad: true,
                asVelocity: true,
                clamp: false
            ).magnitude;

        __instance.IsActiveThisFrame = value > __instance.PressedThreshold;

        return false;
    }
    [HarmonyPrefix, HarmonyPatch(typeof(CompositeInputCommands), nameof(CompositeInputCommands.UpdateFromAction))]
    static bool CompositeInputCommands_UpdateFromAction_Patch(CompositeInputCommands __instance) {
        __instance.PrimaryAction.Update();
        __instance.SecondaryAction.Update();

        __instance.AxisValue = new Vector2(__instance._secondaryAction.MouseValue, __instance._primaryAction.MouseValue);

        __instance.InitializeAxisID();

        var value = __instance
            .GetAxisValue(
                useSensitivity: false,
                useInversionFactor: false,
                includeMouse: true,
                includeGamepad: true,
                asVelocity: true,
                clamp: false
            ).magnitude;

        __instance.IsActiveThisFrame = value > __instance.PressedThreshold;

        return false;
    }

    // The CompositeInputCommands.InitializeAxisID calls the ShouldAccumulate setter, which is unsupported.
    // We use a transpiler to disable the call.
    [HarmonyTranspiler, HarmonyPatch(typeof(CompositeInputCommands), nameof(CompositeInputCommands.InitializeAxisID))]
    static IEnumerable<CodeInstruction> CompositeInputCommands_InitializeAxisID_Transpiler(IEnumerable<CodeInstruction> instructions)
        => instructions.ReplaceMethod(
                AccessTools.PropertySetter(
                    typeof(AbstractCommands),
                    nameof(AbstractCommands.ShouldAccumulate)),
                Enumerable.Repeat(new CodeInstruction(OpCodes.Pop), 2));

    // AbstractInputCommands.GetUITextures unnecessarily checks OWInput.UsingGamepad instead of using the parameter
    // it was given. The code looks like
    //
    // GetUITextures(bool gamepad, bool forceRefresh = false) {
    //     ...
    //     textureList.Clear()
    //     ...
    //     if (gamepad && !OWInput.UsingGamepad()) {
    //         return textureList;
    //     }
    //     // compute actual texture list
    //     ...
    // }
    //
    // We want GetUITextures to actually do what it's told, so we patch it. The simplest way to do this is
    // just to replace the call to UsingGamepad with ldarg.1 (the gamepad argument). Perhaps it would be
    // more efficient to patch out the check entirely, but who cares.
    //
    // Unfortunately, AbstractInputCommands is generic, so we need to individually patch each specialization.
    // As far as I can tell, there are four values of the generic parameter in the codebase:
    // IAxisInputAction, RebindableAxisInputAction, IVectorInputAction, and RebindableInputActionPair.
    //
    // AbstractCompositeInputCommands.GetUITextures has the exact same problem, and we use the exact same patch.
    // As far as I can tell, there is only one specialization in use: IInputActionPair
    //
    // This used to be 184 lines, now 32
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(AbstractInputCommands<IAxisInputAction>), nameof(AbstractInputCommands<IAxisInputAction>.GetUITextures))]
    [HarmonyPatch(typeof(AbstractInputCommands<RebindableAxisInputAction>), nameof(AbstractInputCommands<RebindableAxisInputAction>.GetUITextures))]
    [HarmonyPatch(typeof(AbstractInputCommands<IVectorInputAction>), nameof(AbstractInputCommands<IVectorInputAction>.GetUITextures))]
    [HarmonyPatch(typeof(AbstractInputCommands<RebindableInputActionPair>), nameof(AbstractInputCommands<RebindableInputActionPair>.GetUITextures))]
    [HarmonyPatch(typeof(AbstractCompositeInputCommands<IInputActionPair>), nameof(AbstractCompositeInputCommands<IInputActionPair>.GetUITextures))]
    static IEnumerable<CodeInstruction> AbstractCommands_GetUITextures_Transpiler(IEnumerable<CodeInstruction> instructions)
        => instructions.ReplaceMethod(
                Methods.OWInput_UsingGamepad,
                new CodeInstruction(OpCodes.Ldarg_1));
}

