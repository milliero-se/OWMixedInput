using HarmonyLib;
using OWML.Common;
using System.Reflection;
using System.Reflection.Emit;

using UnityEngine;

namespace OWMixedInput;

[HarmonyPatch]
public static class CameraPatches {

    // Check if the ship allows free look, but take our user setting into account.
    static bool FreeLookAllowed(this PlayerCameraController controller) 
        => OWMixedInput.Settings.freeLook && (controller.ShipController?.AllowFreeLook() ?? false);

    // The vanilla PlayerCharacterController.UpdateTurning method computes the rotation directly from the look inputs.
    // It stores the result in a field _lastTurnInput which is used to animate the player model's limbs when turning.
    //
    // Our look logic is now more complicated with our various improvements. Now, the PlayerCameraController is responsible
    // for computing its rotation and the character controller simply reads the camera controller's angles. Regardless,
    // we still need a value for the _lastTurnInput for the animations, so the PlayerCameraController is now responsible
    // for setting this value on the character controller.
    //
    // With mouse smoothing disabled, this value can be very jittery, which results in choppy animation. Therefore,
    // we use an axis smoothing buffer to smooth it out. It's still not perfect. Perhaps it could be further improved later.
    // But the current approach is good enough for now.
    //
    // This buffer will get reused across expeditions which technically results in stale values, but just for a moment,
    // so who really cares?
    static AxisSmoothingBuffer lastTurnInputBuffer = new();

    // The vanilla camera code treats gamepad and mouse inputs differently.
    // We have unified the control paths in the IInputCommands hierarchy, so we now need to fix the code here.
    [HarmonyPrefix, HarmonyPatch(typeof(PlayerCameraController), nameof(PlayerCameraController.UpdateInput))]
    public static bool PlayerCameraController_UpdateInput_Patch(PlayerCameraController __instance) {
     
        // Vanilla logic does not process camera inputs when snapping.
        // We change this behavior as it is disorienting when using gyro.
        //
        //// Vanilla
        // if (_isSnapping) return;

        // However, we still don't process inputs when locked on.
        if (__instance._isLockedOn) {
            return false;
        }

        // However, we need to make some extra checks that used to be caught by snapping.
        //
        // The only one I have found necessary so far is for the landing cam.
        // The mode mask works for this, except for a fraction of a second.
        if (__instance.ShipController?._usingLandingCam ?? false) {
            return false;
        }

        // We accumulate look inputs here and add them at the end
        var look = Vector2.zero;

        bool freeLook = __instance.FreeLookAllowed() && OWInput.IsPressed(InputLibrary.freeLook);
        bool inLookMode = OWInput.IsInputMode(InputMode.Character | InputMode.ScopeZoom | InputMode.NomaiRemoteCam | InputMode.PatchingSuit);

        // Only processes joystick/mouse look in free look, when controlling the character, outside the suit or in gravity.
        // Additionally, only when not snapping. This is just a fail safe so that your camera doesn't get stuck at a weird angle
        // when you aren't able to move it because you don't have gyro.
        bool useJoystickAndMouse = (freeLook || inLookMode)
            && (!PlayerState.InZeroG() || !PlayerState.IsWearingSuit())
            && !__instance._isSnapping;

        if (useJoystickAndMouse) {
            var mouseInput = OWMixedInput.GetAxisValue(InputLibrary.look, asVelocity: false, includeMouse: true, includeGamepad: false);
            look.x += mouseInput.x;
            look.y += mouseInput.y * OWMixedInput.Settings.mouseSensitivityRatio;

            // Don't process normal joystick inputs when flick stick is enabled
            if (!OWMixedInput.Settings.flickEnabled) {
                var gamepadInput = OWMixedInput.GetAxisValue(InputLibrary.look, asVelocity: false, includeMouse: false, includeGamepad: true);

                look.x += gamepadInput.x;
                look.y += gamepadInput.y * OWMixedInput.Settings.gamepadSensitivityRatio;
            }

            // Multiply by fov scale
            look *= __instance._playerCamera.fieldOfView / __instance._initFOV;

            // Multiply by look rate
            if (freeLook) {
                look *= 180f;
            } else {
                look *= PlayerCameraController.LOOK_RATE;
            }
        }

        var gyroMask = InputMode.Character | InputMode.ScopeZoom | InputMode.NomaiRemoteCam | InputMode.PatchingSuit 
            | InputMode.ShipCockpit;

        // Always use gyro inputs (with the above mask). If gyro is disabled, this is zero.
        var gyroInput = OWMixedInput.GetGyroValue(mask: gyroMask) * MixedInputUtils.RadToDeg;

        look.x -= gyroInput.y;
        look.y += gyroInput.x * OWMixedInput.Settings.gyroSensitivityRatio;

        // Multiply by slow camera scale
        bool alarmIsWakingPlayer = Locator.GetAlarmSequenceController()?.IsAlarmWakingPlayer() ?? false;

        var useSlowCamera = OWMixedInput.Settings.slowCamera
            && (__instance._zoomed || alarmIsWakingPlayer);
        if (useSlowCamera) {
            look *= PlayerCameraController.ZOOM_SCALAR;
        }

        // Now that we are done with all our other scaling factors, add flick stick.
        if (useJoystickAndMouse) {
            // This is zero when flick stick is disabled
            __instance._degreesX -= OWMixedInput.GetFlickValue() * MixedInputUtils.RadToDeg;
        }

        // I don't think this is necessary anymore.
        //// Vanilla
        //if (Time.timeScale > 1f) {
        //    scale /= Time.timeScale;
        //}

        // Add the look inputs to the camera.
        __instance._degreesX += look.x;
        __instance._degreesY += look.y;

        // Update the lastTurnInput buffer. We divide by the look rate and deltaTime to get it in the range
        // that the PlayerCharacterController would normally use.
        lastTurnInputBuffer.Update(look.x / (PlayerCameraController.LOOK_RATE * OWMixedInput.deltaTime));
        // Set the lastTurnInput field on the character controller
        __instance._characterController._lastTurnInput = lastTurnInputBuffer.GetAverage();

        return false;
    }

    // In most circumstances, the player camera is relative to the player character itself.
    // Therefore, the vanilla code usually sets the degreesX for the camera to be zero, expecting
    // the player character to rotate instead. Since the player character only updates in a FixedUpdate,
    // we set degreesX here.
    //
    // The camera Y axis is also clamped to +-80 degress. We expand this to +-89.999.
    [HarmonyPrefix, HarmonyPatch(typeof(PlayerCameraController), nameof(PlayerCameraController.UpdateRotation))]
    public static bool PlayerCameraController_UpdateRotation_Patch(PlayerCameraController __instance) {
        __instance._degreesX %= 360f;
        __instance._degreesY %= 360f;

        // For some reason vanilla does not clamp angles when snapping.
        // We choose to, since we can move the camera with gyro while snapping.
        // We do, however, avoid clamping angles when using the landing cam.
        //
        //// Vanilla
        // if (!_isSnapping) {
        
        // Vanilla uses two snapping angles: free look and default
        //
        // We use ship cockpit (including free look), space walk, and default.
        // Perhaps there should be others.
        if (OWInput.IsInputMode(InputMode.ShipCockpit)) {
            if (!(__instance.ShipController?._usingLandingCam ?? false)) {
                __instance._degreesX = Mathf.Clamp(__instance._degreesX, -60f, 60f);
                __instance._degreesY = Mathf.Clamp(__instance._degreesY, -35f, 80f);
            }
        } 
        else if (OWInput.IsInputMode(InputMode.Character | InputMode.ScopeZoom | InputMode.NomaiRemoteCam | InputMode.PatchingSuit)) {
            if (PlayerState.InZeroG() && PlayerState.IsWearingSuit()) {
                var xLimit = OWMixedInput.Settings.spaceWalkRangeX / 2;
                var yLimit = OWMixedInput.Settings.spaceWalkRangeY / 2;
                __instance._degreesX = Mathf.Clamp(__instance._degreesX, -xLimit, xLimit);
                __instance._degreesY = Mathf.Clamp(__instance._degreesY, -yLimit, yLimit);
            } else {
                __instance._degreesY = Mathf.Clamp(__instance._degreesY, -89.999f, 89.999f);
            }
        }
        
        __instance._rotationX = Quaternion.AngleAxis(__instance._degreesX, Vector3.up);
        __instance._rotationY = Quaternion.AngleAxis(__instance._degreesY, Vector3.left);
        __instance._playerCamera.transform.localRotation = __instance._rotationX * __instance._rotationY;

        return false;
    }

    // The SnapToDegreesOverSeconds method is used to setup state for the snapping method.
    // I found it unpleasant to have camera inputs disabled during snapping, but in the vanilla code, this
    // is a necessary because snapping directly sets the camera angles.
    //
    // As part of these changes, we repurpose the _initSnapTime variable to be the snap progress, defined
    // as the quotient (Time.unscaledTime - initSnapTime) / _snapDuration. Thus, we make this variable
    // begin at zero;
    [HarmonyPostfix, HarmonyPatch(typeof(PlayerCameraController), nameof(PlayerCameraController.SnapToDegreesOverSeconds))]
    public static void PlayerCameraController_SnapToDegreesOverSeconds_Postfix(PlayerCameraController __instance) {
        __instance._initSnapTime = 0f;
    }

    // The UpdateCamera needs its snapping logic updated according to the logic above. 
    // The rest of the behavior is vanilla.
    //
    // The deltaTime passed to this is Time.unscaledDeltaTime
    [HarmonyPrefix, HarmonyPatch(typeof(PlayerCameraController), nameof(PlayerCameraController.UpdateCamera))]
    public static bool PlayerCameraController_UpdateCamera_Patch(PlayerCameraController __instance, float deltaTime) {
        __instance.UpdateInput(deltaTime);

        if (__instance._isLockedOn) {
            __instance.UpdateLockOnTargeting(deltaTime);
        }

        // Our new snapping logic
        if (__instance._isSnapping && !__instance._isLockedOn) {
            // _initSnapTime is repurposed to the snap progress
            float prevProgress = __instance._initSnapTime;

            // Compute new progress and update _initSnapTime
            float progress = Mathf.Min(prevProgress + deltaTime / __instance._snapDuration, 1f);
            __instance._initSnapTime = progress;

            // Apply smooth step
            if (__instance._smoothStepToDegrees) {
                prevProgress = Mathf.SmoothStep(0f, 1f, prevProgress);
                progress = Mathf.SmoothStep(0f, 1f, progress);
            }

            // Done snapping
            if (progress >= 1f) {
                __instance._isSnapping = false;
            }

            var deltaProgress = progress - prevProgress;

            // Accumulate snapping into the camera angles
            __instance._degreesX += (__instance._snapTargetX - __instance._initSnapDegreesX) * deltaProgress;
            __instance._degreesY += (__instance._snapTargetY - __instance._initSnapDegreesY) * deltaProgress;
        }

        var crouchFraction = __instance._characterController.GetJumpCrouchFraction();

        var t = (crouchFraction > 0f) ? 1f : 0.1f;
        __instance._targetLocalPosition = __instance._origLocalPosition - Vector3.up * 0.3f * crouchFraction;
        __instance.transform.localPosition = Vector3.Lerp(__instance.transform.localPosition, __instance._targetLocalPosition, t);
        
        __instance.UpdateFieldOfView(deltaTime);
        __instance.UpdateRotation();

        return false;
    }

    // The vanilla code replicates most of the PlayerCameraController logic in the PlayerCharacterController
    // to turn the player character, which in turn rotates the camera. Since we are updating the camera 
    // in Update instead of FixedUpdate, we just fish the _degreesX value out of the camera controller.
    // Importantly, we must then unset the _degreesX from the camera controller and update it.
    [HarmonyPrefix, HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.UpdateTurning))]
    public static bool PlayerCharacterController_UpdateTurning_Patch(PlayerCharacterController __instance) {
        // Get the rotation from the camera controller instead of from inputs.
        var cameraController = Locator.GetPlayerCameraController();
        var rotationFromCamera = Quaternion.AngleAxis(cameraController._degreesX, __instance._transform.up);

        // _lastTurnInput is handled by the camera controller now.
        //// Vanilla
        //_lastTurnInput = OWInput.GetAxisValue(InputLibrary.look, InputMode.Character | InputMode.ScopeZoom | InputMode.NomaiRemoteCam).x
        //    * _playerCam.fieldOfView / _initFOV;

        // Compute the rotation due to velocity, just like vanilla
        if (__instance._isGrounded && __instance._groundBody != null) {
            var angularVelocity = __instance.MovingPlatform?.GetAngularVelocity() ?? __instance._groundBody.GetAngularVelocity();
            __instance._baseAngularVelocity = Vector3.Dot(angularVelocity, __instance._transform.up);
        } else {
            __instance._baseAngularVelocity *= 0.995f;
        }
        var rotationFromVelocity = Quaternion.AngleAxis(__instance._baseAngularVelocity * 180f / Mathf.PI * Time.deltaTime, __instance._transform.up);

        // Apply the rotations
        __instance._transform.rotation = rotationFromCamera * rotationFromVelocity * __instance._transform.rotation;

        // Now that the player has been rotated, unset the degrees from the camera.
        cameraController._degreesX = 0;

        return false;
    }

    // The vanilla FixedUpdate method is responsible for camera updates when time is not paused.
    // Instead, we do the calculations here so that we can accumulate inputs precicely.
    //
    // We also add support for our re-center button.
    [HarmonyPrefix, HarmonyPatch(typeof(PlayerCameraController), nameof(PlayerCameraController.Update))]
    public static bool PlayerCameraController_Update_Patch(PlayerCameraController __instance) {

        var recenterMask = InputMode.Character | InputMode.ScopeZoom | InputMode.NomaiRemoteCam | InputMode.PatchingSuit | InputMode.ShipCockpit;

        if (__instance.FreeLookAllowed() && OWInput.IsNewlyReleased(InputLibrary.freeLook)) {
            __instance.CenterCameraOverSeconds(0.33f, smoothStep: true);
        } 
        else if (OWInput.IsNewlyPressed(OWMixedInput.Commands.Recenter, mask: recenterMask)) {
            __instance.CenterCameraOverSeconds(OWMixedInput.Settings.recenterTime / 1000f, smoothStep: true);
        }

        __instance.UpdateCamera(Time.unscaledDeltaTime);
        return false;
    }

    // Since we are always handling the camera in Update, we don't need to do anything during FixedUpdate.
    [HarmonyPrefix, HarmonyPatch(typeof(PlayerCameraController), nameof(PlayerCameraController.FixedUpdate))]
    public static bool PlayerCameraController_FixedUpdate_Patch()
        => false;

    // Unity's null comparision is menacing.
    // Are these methods dumb? Yes. Do I care? No.
    extension(PlayerCameraController instance) {
        ShipCockpitController ShipController
            => (instance._shipController != null) ? instance._shipController : null;
    }
    extension(PlayerCharacterController instance) {
        MovingPlatform MovingPlatform
            => (instance._movingPlatform != null) ? instance._movingPlatform : null;
    }
    [HarmonyPostfix, HarmonyPatch(typeof(Locator), nameof(Locator.GetAlarmSequenceController))]
    public static void Locator_GetAlarmSequenceController_Postfix(ref AlarmSequenceController __result) {
        if (__result == null) {
            __result = null;
        }
    }

    // StationaryProbeLauncher uses its own view code.
    // This is essentially the same as vanilla, but we use non-velocity look inputs and flick stick + gyro
    //
    // The vanilla game uses a much smaller look scale here. I decided not to change the scale of flick stick or gyro
    // input because I don't like messing with user settings, but this is possibly a small immersion downgrade.
    // We could consider some sort of smoothing so that the inputs take longer to apply and the probe launcher
    // still feels heavy to move. For such a small part of the game, I decided not to attempt that currently.
    //
    // Vanilla logic also does not use the gamepad sensitivity ratio or fov scale. We don't either, but perhaps we should.
    [HarmonyPrefix, HarmonyPatch(typeof(StationaryProbeLauncher), nameof(StationaryProbeLauncher.UpdateRotation))]
    static bool StationProbeLauncher_UpdateRotation_Patch(StationaryProbeLauncher __instance) {
        // Get mouse input
        var look = 50f * OWMixedInput.GetAxisValue(InputLibrary.look, asVelocity: false, includeGamepad: false);

        // Get gamepad input
        if (OWMixedInput.Settings.flickEnabled) {
            look.x -= OWMixedInput.GetFlickValue() * MixedInputUtils.RadToDeg;
        } else {
            look += 50f * OWMixedInput.GetAxisValue(InputLibrary.look, asVelocity: false, includeMouse: false);
        }

        // Get gyro input. This is zero when gyro is disabled.
        var gyroInput = OWMixedInput.GetGyroValue() * MixedInputUtils.RadToDeg;

        look.x -= gyroInput.y;
        look.y += gyroInput.x * OWMixedInput.Settings.gyroSensitivityRatio;

        if (__instance._lockInputY) {
            look.y = 0f;
        }

        // I'm not entirely sure what the vanilla code is doing here.
        // It's probably still fine with gyro and flick stick involved.
        float localVolume = (__instance._returningToStartPos ? 1f : Mathf.Max(Mathf.Abs(look.x), Mathf.Abs(look.y)));
        __instance._audioSource.SetLocalVolume(localVolume);

        if (__instance._returningToStartPos) {
            __instance._degreesX = Mathf.MoveTowards(__instance._degreesX, 0f, Time.deltaTime * 80f);
            __instance._degreesY = Mathf.MoveTowards(__instance._degreesY, __instance._initialDegreesY, Time.deltaTime * 80f);
            if (__instance._degreesX == 0f && __instance._degreesY == __instance._initialDegreesY) {
                __instance.FinishExitSequence();
            }
        } else {
            __instance._degreesX += look.x;
            __instance._degreesY += look.y;

            // Ensure degreesX in [-180f, 180f]
            // Not sure if this is necessary, but I think
            // this is what vanilla intended to do.
            __instance._degreesX %= 360f;
            if (__instance._degreesX > 180f) {
                __instance._degreesX -= 360f;
            }
            else if (__instance._degreesX < -180f) {
                __instance._degreesX += 360f;
            }

            __instance._degreesY = Mathf.Clamp(__instance._degreesY, -10f, 30f);
        }
        __instance.transform.localRotation = Quaternion.AngleAxis(__instance._degreesX, __instance._localUpAxis) * __instance._initRotX;
        __instance._verticalPivot.localRotation = Quaternion.AngleAxis(__instance._degreesY, -Vector3.right) * __instance._initRotY;

        return false;
    }

    // HUDHelmetAnimator shakes the helmet according to the look input.
    // It makes a call to
    //      OWInput.GetAxisValue(InputLibrary.look)
    // This is not how we process the camera inputs in this mod, so we need to compute a different value.
    //
    // We also let the user disable the camera shake.
    //
    // We don't include the sensitivity ratio in this method. Neither does vanilla.
    static Vector2 GetAxisValueForHelmet(IInputCommands command, InputMode mask) {
        if (!OWMixedInput.Settings.helmetShake) {
            return Vector2.zero;
        }

        // Get mouse input
        var look = OWMixedInput.GetAxisValue(command, mask: mask, asVelocity: true, clamp: false, includeGamepad: false);

        // Get gamepad input
        if (OWMixedInput.Settings.flickEnabled) {
            // Divide raw angles by the look rate to get everything in the same range
            look.x -= OWMixedInput.GetFlickValue(mask: mask, asVelocity: true, clamp: false) * MixedInputUtils.RadToDeg
                / PlayerCameraController.LOOK_RATE;
        } else {
            look += OWMixedInput.GetAxisValue(command, mask: mask, asVelocity: true, clamp: false, includeMouse: false);
        }

        // Get gyro input. This is zero when gyro is disabled.
        // Divide raw angles by the look rate to get everything in the same range
        look += OWMixedInput.GetGyroValue(mask: mask, asVelocity: true, clamp: false) * MixedInputUtils.RadToDeg
            / PlayerCameraController.LOOK_RATE;

        return look;
    }

    [HarmonyTranspiler, HarmonyPatch(typeof(HUDHelmetAnimator), nameof(HUDHelmetAnimator.Update))]
    static IEnumerable<CodeInstruction> HUDHelmetAnimator_Update_Transpiler(IEnumerable<CodeInstruction> instructions)
        => instructions.ReplaceMethod(
                Methods.OWInput_GetAxisValue,
                AccessTools.Method(typeof(CameraPatches), nameof(CameraPatches.GetAxisValueForHelmet)));

    // We provide a user setting for enabling/disable the vanilla free look functionality.
    // Other than the camera code above, usage of the free look command is confined to two methods:
    // ShipCockpitController.Update and ShipPromptController.Update
    //
    // Both methods check if the free look key is pressed for behavior regarding the launch camera,
    // which is disabled while free looking. They do so using
    //      OWInput.IsPressed(InputLibrary.freeLook)
    //
    // Additionally, the ShipPromptController.Update method calls
    //      _freeLookPrompt.SetVisibility(...)
    // to show/hide the free look prompt.
    //
    // Using transpiler patches, we intercept these method calls to disable free look functionality
    // when the user free look setting is disabled.
    // Perhaps these patches belong in another file, but they are kept here to keep the free look
    // code in one place.

    static bool IsPressedForFreeLook(IInputCommands command, float minPressDuration)
        => OWMixedInput.Settings.freeLook && OWInput.IsPressed(command, minPressDuration);
    static void SetVisibilityForFreeLook(ScreenPrompt prompt, bool visibility) {
        prompt.SetVisibility(OWMixedInput.Settings.freeLook && visibility);
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(ShipCockpitController), nameof(ShipCockpitController.Update))]
    [HarmonyPatch(typeof(ShipPromptController), nameof(ShipPromptController.Update))]
    static IEnumerable<CodeInstruction> IsPressed_FreeLook_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod) {
        var result = instructions.ToList();

        // The two relevant instructions
        var freeLookGetter = AccessTools.PropertyGetter(typeof(InputLibrary), nameof(InputLibrary.freeLook));
        var isPressedMethod = AccessTools.Method(typeof(OWInput), nameof(OWInput.IsPressed), [typeof(IInputCommands), typeof(float)]);
        
        var count_found = 0;
        // Start at 2, since we look back two instructions
        for (int i = 2; i < result.Count; i++) {
            var instruction = result[i];

            // We are looking to replace OWInput.IsPressed(InputLibrary.freeLook)
            // Skip anything else
            if (!isPressedMethod.MatchInstruction(instruction)) {
                continue;
            }
            // Previous instruction is ldc.r4 0f
            // But before that, we check for InputLibrary.freeLook
            if (!freeLookGetter.MatchInstruction(result[i-2])) {
                continue;
            }

            // We found OWInput.IsPressed(InputLibrary.freeLook)
            count_found += 1;

            // Just change operand. Preserves labels and such.
            instruction.operand = AccessTools.Method(typeof(CameraPatches), nameof(CameraPatches.IsPressedForFreeLook));
        }

        if (count_found != 1) {
            OWMixedInput.WriteLine($"IsPressed_FreeLook_Transpiler: The structure of {originalMethod} is unexpected. Refusing to patch it.", 
                    MessageType.Error);
            return instructions;
        }

        return result;
    }

    [HarmonyTranspiler, HarmonyPatch(typeof(ShipPromptController), nameof(ShipPromptController.Update))]
    static IEnumerable<CodeInstruction> SetVisibility_FreeLook_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod) {
        var result = instructions.ToList();

        var freeLookPromptField = AccessTools.Field(typeof(ShipPromptController), nameof(ShipPromptController._freeLookPrompt));
        var setVisibilityMethod = AccessTools.Method(typeof(ScreenPrompt), nameof(ScreenPrompt.SetVisibility));

        var count_found = 0;
        // Start at 8, since we look back 8 instructions
        for (int i = 8; i < result.Count; i++) {
            var instruction = result[i];

            // We are looking for _freeLookPrompt.SetVisibility(...)
            // Skip anything else
            if (!setVisibilityMethod.MatchInstruction(instruction)) {
                continue;
            }

            // We are looking for ldlfd _freeLookPrompt 8 instructions prior
            var potentialLoad = result[i-8];
            if (!(potentialLoad.opcode == OpCodes.Ldfld && (FieldInfo)potentialLoad.operand == freeLookPromptField)) {
                continue;
            }

            // We found it!
            count_found += 1;

            // Use call instead of callvirt
            // Otherwise, preserve labels and such.
            instruction.opcode = OpCodes.Call;
            instruction.operand = AccessTools.Method(typeof(CameraPatches), nameof(CameraPatches.SetVisibilityForFreeLook));
        }

        if (count_found != 1) {
            OWMixedInput.WriteLine($"SetVisibility_FreeLook_Transpiler: The structure of {originalMethod} is unexpected. Refusing to patch it.", 
                    MessageType.Error);
            return instructions;
        }

        return result;
    }
}
