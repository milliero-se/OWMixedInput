using HarmonyLib;
using System.Reflection;
using System.Reflection.Emit;

using UnityEngine;

namespace OWMixedInput;

// Patches for various input methods
[HarmonyPatch]
public static class InputPatches {
    // Disable the custom vanilla input processors/interactions. While unnecessary, it helps catch bugs.
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(OWAxisProcessor), nameof(OWAxisProcessor.Process))]
    [HarmonyPatch(typeof(OWDoubleAxisProcessor), nameof(OWDoubleAxisProcessor.Process))]
    static IEnumerable<CodeInstruction> DoNotProcess()
        => [new CodeInstruction(OpCodes.Ldarg_1), new CodeInstruction(OpCodes.Ret)];

    // SettingsSave calls UsingGamepad to when setting sensitivity settings, which is somewhat insane.
    // We account for this behavior difference manually, so SettingsSave can act as if UsingGamepad is true.
    [HarmonyTranspiler, HarmonyPatch(typeof(SettingsSave), nameof(SettingsSave.UpdateSensitivitySettings))]
    static IEnumerable<CodeInstruction> SettingsSave_UpdateSensitivitySettings_Transpiler(IEnumerable<CodeInstruction> instructions)
        => instructions.ReplaceMethod(
                Methods.OWInput_UsingGamepad,
                new CodeInstruction(OpCodes.Ldc_I4_1));

    // The vanilla RoastingStickController.UpdateExtension method calls UsingGamepad to choose analog or binary
    // control of the roasting stick. However, because the position is lerped into place either way, there is
    // no reason not to use the analog behavior at all times. Thus, we replace the UsingGamepad call with
    // the constant true. It would be more efficient to skip the branching entirely, but who cares.
    [HarmonyTranspiler, HarmonyPatch(typeof(RoastingStickController), nameof(RoastingStickController.UpdateExtension))]
    static IEnumerable<CodeInstruction> RoastingStickController_UpdateExtension_Transpiler(IEnumerable<CodeInstruction> instructions)
        => instructions.ReplaceMethod(
                Methods.OWInput_UsingGamepad,
                new CodeInstruction(OpCodes.Ldc_I4_1));

    // The roasting stick controller gets look input in its UpdateRotation method, and also calls UsingGamepad.
    // The code looks like
    //
    //  Vector2 axisValue = OWInput.GetAxisValue(InputLibrary.look, InputMode.Roasting);
    //  float num = (OWInput.UsingGamepad() ? Mathf.Lerp(5f, 1f, _extendFraction) : Mathf.Lerp(2f, 1f, _extendFraction));
    //  _cursorPos += axisValue * num * Time.deltaTime;
    //
    // We replace the GetAxisValue call with a call non-velocity call, including gyro, replace Time.deltaTime with 1,
    // and replace UsingGamepad with a user setting.

    static Vector2 GetAxisValueForRoastingStick(IInputCommands command, InputMode mask) {
        var result = OWMixedInput.GetAxisValue(
            command,
            mask: mask,
            asVelocity: false,
            clamp: false);

        // Divide by look rate to get everything in the same range
        var gyroValue = OWMixedInput.GetGyroValue(mask) * MixedInputUtils.RadToDeg
            / PlayerCameraController.LOOK_RATE;

        result.x -= gyroValue.y;
        result.y += gyroValue.x;

        return result;
    }

    static bool UsingGamepadForRoastingStick() 
        => OWMixedInput.Settings.roastingStick.Match(
            Default: OWMixedInput.UsingGamepadForPrimary(),
            Vanilla: OWMixedInput.LastInputUsingGamepad(),
            Gamepad: true,
            KBM: false
        );

    [HarmonyTranspiler, HarmonyPatch(typeof(RoastingStickController), nameof(RoastingStickController.UpdateRotation))]
    static IEnumerable<CodeInstruction> RoastingStickController_UpdateRotation_Transpiler(IEnumerable<CodeInstruction> instructions)
        => instructions
                .ReplaceMethod(
                        Methods.OWInput_GetAxisValue, 
                        AccessTools.Method(typeof(InputPatches), nameof(InputPatches.GetAxisValueForRoastingStick)))
                .ReplaceMethod(
                        Methods.OWInput_UsingGamepad, 
                        AccessTools.Method(typeof(InputPatches), nameof(InputPatches.UsingGamepadForRoastingStick)))
                .ReplaceMethod(
                        Methods.Time_deltaTime, 
                        new CodeInstruction(OpCodes.Ldc_R4, 1.0f));

    // The MapController class calls InputLibrary.look.GetAxisValue(useSensitivity: false) in LateUpdate.
    // There is no reason to clamp the mouse values, so we don't.
    //
    // This replacement does result in small precision loss for mouse inputs due to dividing and multiplying
    // by the update time, but this shouldn't be noticable, and precision in the map is not super important.
    //
    // The alternative is simply too complicated. MapController.LastUpdate is a very large method.

    static Vector2 GetAxisValueForMapController(IInputCommands command, bool useSensitivity)
        => command.GetAxisValue(
                useSensitivity: useSensitivity,
                clamp: false);

    [HarmonyTranspiler, HarmonyPatch(typeof(MapController), nameof(MapController.LateUpdate))]
    static IEnumerable<CodeInstruction> MapController_LateUpdate_Transpiler(IEnumerable<CodeInstruction> instructions) 
        => instructions.ReplaceMethod(
                Methods.IInputCommands_GetAxisValue,
                AccessTools.Method(typeof(InputPatches), nameof(InputPatches.GetAxisValueForMapController)));

    // The ShipLogEntryDescriptionField class uses UsingGamepad to determined how it is controlled.
    // We allow the user to override this behavior.
    //
    // When this is set to gamepad, the look inputs are used to scroll the log. This can result in small
    // precision loss with mouse inputs, but this should not be noticable.

    static bool UsingGamepadForShipLog()
        => OWMixedInput.Settings.shipLog.Match(
            Default: OWMixedInput.UsingGamepadForPrimary(),
            Vanilla: OWMixedInput.LastInputUsingGamepad(),
            Gamepad: true,
            KBM: false
        );

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(ShipLogEntryDescriptionField), nameof(ShipLogEntryDescriptionField.Start))]
    [HarmonyPatch(typeof(ShipLogEntryDescriptionField), nameof(ShipLogEntryDescriptionField.Update))]
    static IEnumerable<CodeInstruction> ShipLogEntryDescriptionField_Transpiler(IEnumerable<CodeInstruction> instructions)
        => instructions.ReplaceMethod(
                Methods.OWInput_UsingGamepad,
                AccessTools.Method(typeof(InputPatches), nameof(InputPatches.UsingGamepadForShipLog)));

    // The ShipCockpitController class uses UsingGamepad to determined when autopilot is enabled.
    // Using gamepad controls, the autopilot is disabled when a probe is anchored to avoid overlapping controls.
    // We allow the user to override this setting.
    
    static bool UsingGamepadForAutopilot()
        => OWMixedInput.Settings.autopilot.Match(
            Default: OWMixedInput.UsingGamepadForPrimary(),
            Vanilla: OWMixedInput.LastInputUsingGamepad(),
            Gamepad: true,
            KBM: false
        );

    [HarmonyTranspiler, HarmonyPatch(typeof(ShipCockpitController), nameof(ShipCockpitController.IsAutopilotAvailable))]
    static IEnumerable<CodeInstruction> ShipCockpitController_IsAutopilotAvailable_Transpiler(
            IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        => instructions.ReplaceMethod(
                Methods.OWInput_UsingGamepad,
                AccessTools.Method(typeof(InputPatches), nameof(InputPatches.UsingGamepadForAutopilot)),
                __originalMethod);

    // The ProbeLauncher and ProbePromptController calls
    //   InputLibrary.toolActionPrimary.HasSameBinging(InputLibrary.probeRetrieve, OWInput.UsingGamepad())
    // to determine if the probe retrieve input needs to be held in order to retrieve the probe.
    // In the default controls, these controls have the same bindings. If no check was performed,
    // the toolPrimaryAction command could not be used to take probe pictures, as it would retrieve the probe.
    // This check is made in three methods: ProbeLauncher.UpdatePostLaunch, ProbePromptController.Awake,
    // and ProbePromptController.Update.
    //
    // The ProbeLauncher calls
    //   InputLibrary.toolActionSecondary.HasSameBinding(InputLibrary.probeRetrieve, OWInput.UsingGamepad())
    // to check if the reverse camera is enabled. If these controls do not overlap, the reverse camera is enabled.
    //
    // This behavior is not desirable for two primary reasons: first, the dependence on OWInput.UsingGamepad
    // is unacceptable in the context of this mod, and second, the reverse camera could easily be kept available
    // by requiring the probe retrieve to be held.
    //
    // To hook this behavior, we simply replace the call to HasSameBinding with 3 pops and a call to our own boolean
    // methods ProbeHoldToRetrieve and ProbeReverseCamUnavailable.

    static bool HasSameBindingForProbe(IInputCommands left, IInputCommands right) {
        var gamepad = left.HasSameBinding(right, usingGamepad: true);
        var kbm = left.HasSameBinding(right, usingGamepad: false);

        return OWMixedInput.Settings.probeLauncher.Match(
            Default: OWMixedInput.UsingGamepadForPrimary() ? gamepad : kbm,
            Vanilla: OWMixedInput.LastInputUsingGamepad() ? gamepad : kbm,
            Hybrid: gamepad || kbm,
            Gamepad: gamepad,
            KBM: kbm
        );
    }

    static bool ProbeHoldToRetrieve() {
        var primaryOverlap = HasSameBindingForProbe(InputLibrary.toolActionPrimary, InputLibrary.probeRetrieve);
        var secondaryOverlap = HasSameBindingForProbe(InputLibrary.toolActionSecondary, InputLibrary.probeRetrieve);

        return OWMixedInput.Settings.probeOverlaps.Match(
            Primary: primaryOverlap,
            Both: primaryOverlap || secondaryOverlap
        );
    }
    static bool ProbeReverseCamUnavailable() 
        => OWMixedInput.Settings.probeOverlaps.Match(
            Primary: HasSameBindingForProbe(InputLibrary.toolActionSecondary, InputLibrary.probeRetrieve),
            Both: false
        );

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(ProbeLauncher), nameof(ProbeLauncher.UpdatePostLaunch))]
    [HarmonyPatch(typeof(ProbePromptController), nameof(ProbePromptController.Awake))]
    [HarmonyPatch(typeof(ProbePromptController), nameof(ProbePromptController.Update))]
    static IEnumerable<CodeInstruction> Probe_HoldToRetrieve_Transpiler(IEnumerable<CodeInstruction> instructions)
        => instructions.ReplaceMethod(
                AccessTools.Method(typeof(IInputCommands), nameof(IInputCommands.HasSameBinding)),
                Enumerable.Repeat(new CodeInstruction(OpCodes.Pop), 3) // Pop the two commands and the usingGamepad flag
                    .Append(AccessTools.Method(typeof(InputPatches), nameof(InputPatches.ProbeHoldToRetrieve)).Call)
            );

    [HarmonyTranspiler, HarmonyPatch(typeof(ProbeLauncher), nameof(ProbeLauncher.IsReverseCamAvailable))]
    static IEnumerable<CodeInstruction> Probe_ReverseCamAvailable_Transpiler(IEnumerable<CodeInstruction> instructions)
        => instructions.ReplaceMethod(
                AccessTools.Method(typeof(IInputCommands), nameof(IInputCommands.HasSameBinding)),
                Enumerable.Repeat(new CodeInstruction(OpCodes.Pop), 3) // Pop the two commands and the usingGamepad flag
                    .Append(AccessTools.Method(typeof(InputPatches), nameof(InputPatches.ProbeReverseCamUnavailable)).Call)
            );

    // The RumbleManager class only rumbles the controller when OWInput.UsingGamepad() returns true.
    // Here, we patch it so that it follows the mod setting instead.

    static bool UsingGamepadForRumble()
        => OWMixedInput.Settings.rumble.Match(
                Default: OWMixedInput.UsingGamepadForPrimary(),
                Vanilla: OWMixedInput.LastInputUsingGamepad(),
                Yes: true,
                No: false
            );

    static MethodBase UsingGamepadForRumbleMethod
        => AccessTools.Method(typeof(InputPatches), nameof(InputPatches.UsingGamepadForRumble));

    [HarmonyTranspiler, HarmonyPatch(typeof(RumbleManager), nameof(RumbleManager.Update))]
    static IEnumerable<CodeInstruction> RumbleManager_Update_Transpiler(IEnumerable<CodeInstruction> instructions)
        => instructions.ReplaceMethod(Methods.OWInput_UsingGamepad, UsingGamepadForRumbleMethod);

    // RumbleManager.TriggerEffect checks the AxisID of a command to determine if it should apply trigger effects.
    // Since that AxisID might be the KB&M axis based on the UI setting, we patch it to always use the gamepad axes.
    // The AxisID check occurs twice each in two methods: AffectsLeftTrigger & AffectsRightTrigger.
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(RumbleManager.TriggerEffect), nameof(RumbleManager.TriggerEffect.AffectsLeftTrigger))]
    [HarmonyPatch(typeof(RumbleManager.TriggerEffect), nameof(RumbleManager.TriggerEffect.AffectsRightTrigger))]
    static IEnumerable<CodeInstruction> RumbleManager_TriggerEffect_AffectsTrigger_Transpiler(IEnumerable<CodeInstruction> instructions)
        => instructions.ReplaceMethod(
                AccessTools.PropertyGetter(typeof(IInputCommands), nameof(IInputCommands.AxisID)),
                AccessTools.Method(typeof(InputCommandsPatches), nameof(InputCommandsPatches.GamepadAxisID)));
}

