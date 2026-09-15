using HarmonyLib;
using System.Reflection;

using UnityEngine;
using Steamworks;

namespace OWMixedInput;

[HarmonyPatch]
public static class UIPatches {
    // CursorManager.RefreshCursorState never shows the cursor when OWInput.UsingGamepad().
    // We allow the user to override behavior with a setting.

    public static bool UsingGamepadForCursor() =>
        OWInput.IsGamepadEnabled() &&
        OWMixedInput.Settings.cursor.Match(
            Default: OWMixedInput.UsingGamepadForPrimary(),
            Vanilla: OWMixedInput.LastInputUsingGamepad(),
            Yes: false,
            No: true
        );

    [HarmonyTranspiler, HarmonyPatch(typeof(CursorManager), nameof(CursorManager.RefreshCursorState))]
    static IEnumerable<CodeInstruction> CursorManager_RefreshCursorState_Transpiler(IEnumerable<CodeInstruction> instructions)
        => instructions.ReplaceMethod(
                Methods.OWInput_UsingGamepad, 
                AccessTools.Method(typeof(UIPatches), nameof(UIPatches.UsingGamepadForCursor)));

    // PopupInputMenu.TryOpenVirtualKeyboard only opens the keyboard when running on a SteamDeck.
    // We allow the user to override this behavior.
    // Note that the steam virtual keyboard can only open in Big Picture Mode.

    static bool ShouldUseVirtualKeyboard() =>
        OWMixedInput.Settings.virtualKeyboard.Match(
            Vanilla: SteamUtils.IsSteamRunningOnSteamDeck(),
            Yes: true,
            No: false
        );

    [HarmonyTranspiler, HarmonyPatch(typeof(PopupInputMenu), nameof(PopupInputMenu.TryOpenVirtualKeyboard))]
    static IEnumerable<CodeInstruction> PopupInputMenu_TryOpenVirtualKeyboard_Transpiler(IEnumerable<CodeInstruction> instructions)
        => instructions.ReplaceMethod(
                AccessTools.Method(typeof(SteamUtils), nameof(SteamUtils.IsSteamRunningOnSteamDeck)),
                AccessTools.Method(typeof(UIPatches), nameof(UIPatches.ShouldUseVirtualKeyboard)));

    // The roll UI prompt should match the user roll axis setting.
    // This is relevant to ShipPromptController and JetpackPromptController.
    //
    // Unfortunately, there is no way to have three commands on a prompt afaict,
    // so the Y-axis prompt is not as accurate as possible.
    
    static ScreenPrompt MakeRollPrompt() 
        => new ScreenPrompt(
            InputLibrary.rollMode, 
            OWMixedInput.Settings.rollAxis.Match(
                Yaw: InputLibrary.yaw,
                X: InputLibrary.thrustX,
                Y: InputLibrary.thrustUp),
            "<CMD1>" 
                + UITextLibrary.GetString(UITextType.HoldPrompt) 
                + "  +<CMD2>   " 
                + UITextLibrary.GetString(UITextType.RollPrompt), 
            ScreenPrompt.MultiCommandType.CUSTOM_BOTH);

    // Update the roll prompt on a prompt controller.
    // We only mess with the prompt manager if the prompts have already been initialized.
    static void UpdateRollPrompt(this ShipPromptController controller) {
        if (controller._screenPromptsInitialized) {
            Locator.GetPromptManager().RemoveScreenPrompt(controller._rollPrompt, PromptPosition.UpperLeft); 
            // We re-add the free look prompt to keep the relative ordering the same.
            Locator.GetPromptManager().RemoveScreenPrompt(controller._freeLookPrompt, PromptPosition.UpperLeft); 
        }
        controller._rollPrompt = MakeRollPrompt();
        if (controller._screenPromptsInitialized) {
            Locator.GetPromptManager().AddScreenPrompt(controller._rollPrompt, PromptPosition.UpperLeft); 
            Locator.GetPromptManager().AddScreenPrompt(controller._freeLookPrompt, PromptPosition.UpperLeft); 
        }
    }
    static void UpdateRollPrompt(this JetpackPromptController controller) {
        if (controller._screenPromptsInitialized) {
            Locator.GetPromptManager().RemoveScreenPrompt(controller._rollPrompt, PromptPosition.UpperRight); 
        }
        controller._rollPrompt = MakeRollPrompt();
        if (controller._screenPromptsInitialized) {
            Locator.GetPromptManager().AddScreenPrompt(controller._rollPrompt, PromptPosition.UpperRight); 
        }
    }

    // After awake, update the roll prompt.
    // The roll prompt should also be updated when changing settings, so subscribe to OnUpdateInputSettings.
    [HarmonyPostfix, HarmonyPatch(typeof(ShipPromptController), nameof(ShipPromptController.Awake))]
    static void ShipPromptController_Awake_Postfix(ShipPromptController __instance) {
        __instance.UpdateRollPrompt();
        OWMixedInput.InputManager.OnUpdateInputSettings += __instance.UpdateRollPrompt;
    }
    [HarmonyPostfix, HarmonyPatch(typeof(JetpackPromptController), nameof(JetpackPromptController.Awake))]
    static void JetpackPromptController_Awake_Postfix(JetpackPromptController __instance) {
        __instance.UpdateRollPrompt();
        OWMixedInput.InputManager.OnUpdateInputSettings += __instance.UpdateRollPrompt;
    }

    // On destroy, unsubscribe from OnUpdateInputSettings.
    [HarmonyPrefix, HarmonyPatch(typeof(ShipPromptController), nameof(ShipPromptController.OnDestroy))]
    static void ShipPromptController_OnDestroy_Prefix(ShipPromptController __instance) {
        OWMixedInput.InputManager.OnUpdateInputSettings -= __instance.UpdateRollPrompt;
    }
    [HarmonyPrefix, HarmonyPatch(typeof(JetpackPromptController), nameof(JetpackPromptController.OnDestroy))]
    static void JetpackPromptController_OnDestroy_Prefix(JetpackPromptController __instance) {
        OWMixedInput.InputManager.OnUpdateInputSettings -= __instance.UpdateRollPrompt;
    }
}
