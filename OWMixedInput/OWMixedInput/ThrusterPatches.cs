using HarmonyLib;

using UnityEngine;

namespace OWMixedInput;

[HarmonyPatch]
public static class ThrusterPatches {
    // The ThrusterModel makes a call to UsingGamepad to determine the maximum rotational thruster strength.
    // This is pretty weird. Regardless, we let the user override this behavior.
    //
    // UsingGamepad => normal range, !UsingGamepad => extended range
    static bool UsingGamepadForRotationalThrusters()
        => !OWMixedInput.Settings.extendRotationalThrusters;

    [HarmonyTranspiler, HarmonyPatch(typeof(ThrusterModel), nameof(ThrusterModel.FireRotationalThrusters))]
    static IEnumerable<CodeInstruction> ThrusterModel_FireRotationalThrusters_Transpiler(IEnumerable<CodeInstruction> instructions)
        => instructions.ReplaceMethod(
                Methods.OWInput_UsingGamepad,
                AccessTools.Method(typeof(ThrusterPatches), nameof(ThrusterPatches.UsingGamepadForRotationalThrusters)));

    // Methods for reading thruster inputs. There are 5 axes of inputs to read:
    // Pitch/yaw pair, XZ pair, single Y.
    //
    // The gamepad part of the two pairs is conditioned according to user settings.
    // Mouse inputs are passed through directly, after converting to a velocity
    static Vector2 ReadPair(IInputCommands first, IInputCommands second, bool squareify) {
        // We do our own clamping
        var gamepad = new Vector2(
            OWMixedInput.GetValue(first, clamp: false, includeMouse: false),
            OWMixedInput.GetValue(second, clamp: false, includeMouse: false));

        // Process gamepad inputs according to squareify parameter.
        MixedInputUtils.ClampMagnitude(ref gamepad);
        if (squareify) {
            MixedInputUtils.Squareify(ref gamepad);
        }

        // Mouse inputs are not clamped or processed.
        var mouse = new Vector2(
            OWMixedInput.GetValue(first, clamp: false, includeGamepad: false),
            OWMixedInput.GetValue(second, clamp: false, includeGamepad: false));

        // Mouse inputs are not processed
        return gamepad + mouse;
    }

    // Read the pitch/yaw axis pair.
    static Vector2 ReadPitchYaw() 
        => ReadPair(InputLibrary.pitch, InputLibrary.yaw, OWMixedInput.Settings.squareifyPitchYaw);

    // Read the XZ axis pair.
    static Vector2 ReadXZ()
        => ReadPair(InputLibrary.thrustX, InputLibrary.thrustZ, OWMixedInput.Settings.squareifyXZ);

    // Read the Y axis. 
    // We need to subtract up and down, but otherwise nothing fancy.
    static float ReadY()
        => OWMixedInput.GetValue(InputLibrary.thrustUp, clamp: false) 
            - OWMixedInput.GetValue(InputLibrary.thrustDown, clamp: false);

    // There are three thruster controllers: ModelShipController, ShipThrusterController, and JetpackThrusterController.
    // They use two methods, ReadRotationalInput and ReadTranslationalInput, to obtain and process user inputs.
    // Generally, the results are expected to be in cube [-1,1]^3, but if the user has set ExtendRotationalThrusters,
    // then the rotational thrust can be in the range [-2,2]^3. Inputs are clamped to this range in ThrusterModel,
    // so we don't need to do so ourselves.
  
    // The model ship is the easiest of the three.
    //
    // Model ship rotation.
    [HarmonyPrefix, HarmonyPatch(typeof(ModelShipController), nameof(ModelShipController.ReadRotationalInput))]
    static bool ModelShipController_ReadRotationalInput_Patch(ModelShipController __instance, ref Vector3 __result) {

        __result = Vector3.zero;

        if (!OWInput.IsInputMode(InputMode.ModelShip)) {
            return false;
        }        

        var pitchYaw = ReadPitchYaw();

        __result.x = -pitchYaw.x;
        // Yaw is bound to roll here. Whatever ig.
        __result.z = -pitchYaw.y;

        return false;
    }
    // Model ship translation
    [HarmonyPrefix, HarmonyPatch(typeof(ModelShipController), nameof(ModelShipController.ReadTranslationalInput))]
    static bool ModelShipController_ReadTranslationalInput_Patch(ModelShipController __instance, ref Vector3 __result) {

        __result = Vector3.zero;

        if (!OWInput.IsInputMode(InputMode.ModelShip)) {
            return false;
        }

        var xz = ReadXZ();
        var y = ReadY();

        __result.x = xz.x;
        __result.y = y;
        __result.z = xz.y;

        return false;
    }

    // The ShipThrusterController and the JetpackThrusterController both have a _isRollMode field.
    // In vanilla, this is used to indicate that the yaw inputs should be used as roll.
    // We let the user pick this axis: Yaw, X, or Y. Consequently, both rotation and translation
    // input processing must be aware of the roll mode.
    //
    // ShipThrusterController and JetpackThrusterController use the same logic, so we provide
    // common methods for reading rotation and translation.
    //
    // rollScalar comes from vanilla code, it's +-1.
    static Vector3 ReadRotation(bool rollMode, int rollScalar) {
        var rotation = Vector3.zero;

        var pitchYaw = ReadPitchYaw();

        // Always use pitch.
        rotation.x = -pitchYaw.x;

        // Use yaw if it isn't used for roll
        bool yawIsRoll = rollMode && OWMixedInput.Settings.rollAxis.isYaw;
        if (!yawIsRoll) {
            // Yaw isn't negated
            rotation.y = pitchYaw.y;
        }

        if (rollMode) {
            // Must read both X and Z to correctly apply user processing.
            var xz = ReadXZ();
            var y = ReadY();

            // Add our own invertRoll setting.
            rollScalar *= OWMixedInput.Settings.invertRoll ? -1 : 1;

            rotation.z = -OWMixedInput.Settings.rollAxis.Match(Yaw: pitchYaw.y, X: xz.x, Y: y) * rollScalar;
        }

        return rotation;
    }
    // Translation doesn't need rollScalar, but it needs rollMode to possibly mask X/Y.
    static Vector3 ReadTranslation(bool rollMode) {
        var translation = Vector3.zero;

        var xz = ReadXZ();
        var y = ReadY();

        // Use x if it isn't used for roll
        bool xIsRoll = rollMode && OWMixedInput.Settings.rollAxis.isX;
        if (!xIsRoll) {
            translation.x = xz.x;
        }

        // Use y if it isn't used for roll
        bool yIsRoll = rollMode && OWMixedInput.Settings.rollAxis.isY;
        if (!yIsRoll) {
            translation.y = y;
        }

        // Always use z
        translation.z = xz.y;

        return translation;
    }

    // With these helper methods, the jetpack methods are pretty simple.
    // 
    // Jetpack rotation
    [HarmonyPrefix, HarmonyPatch(typeof(JetpackThrusterController), nameof(JetpackThrusterController.ReadRotationalInput))]
    static bool JetpackThrusterController_ReadRotationalInput_Patch(JetpackThrusterController __instance, ref Vector3 __result) {

        __result = Vector3.zero;

        if (!OWInput.IsInputMode(InputMode.Character | InputMode.ScopeZoom | InputMode.NomaiRemoteCam)) {
            return false;
        }

        __result = ReadRotation(__instance._isRollMode, __instance._rollScalar); 

        // Should this be affected by the slow camera setting?
        if (OWInput.IsInputMode(InputMode.ScopeZoom)) { 
            __result *= Locator.GetPlayerCameraController().GetZoomFOVRatio(); 
        } 

        return false;
    }
    // Jetpack translation.
    // This is encapsulated in GetRawInput, which is called in ReadTranslationalInput.
    [HarmonyPrefix, HarmonyPatch(typeof(JetpackThrusterController), nameof(JetpackThrusterController.GetRawInput))]
    static bool JetpackThrusterController_GetRawInput_Patch(JetpackThrusterController __instance, ref Vector3 __result) {
    
        __result = Vector3.zero;

        if (!OWInput.IsInputMode(InputMode.Character | InputMode.NomaiRemoteCam)) {
            return false;
        }

        __result = ReadTranslation(__instance._isRollMode);
        
        return false;
    }

    // Ship rotation
    [HarmonyPrefix, HarmonyPatch(typeof(ShipThrusterController), nameof(ShipThrusterController.ReadRotationalInput))]
    static bool ShipThrusterController_ReadRotationalInput_Patch(ShipThrusterController __instance, ref Vector3 __result) {

        __result = Vector3.zero;

        if (!OWInput.IsInputMode(InputMode.ShipCockpit | InputMode.LandingCam)) {
            return false;
        }
        if (!__instance._shipResources.AreThrustersUsable()) {
            return false;
        }
        if (OWInput.IsInputMode(InputMode.ShipCockpit) && OWInput.IsPressed(InputLibrary.freeLook)) {
            return false;
        }
        if (__instance._landingManager.IsLanded()) {
            return false;
        }

        __result = ReadRotation(__instance._isRollMode, __instance._rollScalar); 

        return false;
    }
    // Ship translation
    //
    // This one is the most complicated because it involes takeoff and landing code.
    [HarmonyPrefix, HarmonyPatch(typeof(ShipThrusterController), nameof(ShipThrusterController.ReadTranslationalInput))]
    static bool ShipThrusterController_ReadTranslationalInput_Patch(ShipThrusterController __instance, ref Vector3 __result) {

        __result = Vector3.zero;

        if (!OWInput.IsInputMode(InputMode.ShipCockpit | InputMode.LandingCam)) {
            return false;
        }
        if (!__instance._shipResources.AreThrustersUsable()) {
            return false;
        }
        if (__instance._autopilot.IsFlyingToDestination()) {
            return false;
        }

        __result = ReadTranslation(__instance._isRollMode); 

        // Takeoff code.
        if (__instance._requireIgnition && __instance._landingManager.IsLanded()) {
            __result.x = 0f;
            __result.z = 0f;
            __result.y = Mathf.Clamp01(__result.y);

            if (!__instance._isIgniting && __instance._lastTranslationalInput.y <= 0f && __result.y > 0f) {
                __instance._isIgniting = true;
                __instance._ignitionTime = Time.time;
                GlobalMessenger.FireEvent("StartShipIgnition");
            }

            if (__instance._isIgniting) {
                if (__result.y <= 0f) {
                    __instance._isIgniting = false;
                    GlobalMessenger.FireEvent("CancelShipIgnition");
                }
                if (Time.time < __instance._ignitionTime + __instance._ignitionDuration) {
                    __result.y = 0f;
                } else {
                    __instance._isIgniting = false;
                    __instance._requireIgnition = false;
                    GlobalMessenger.FireEvent("CompleteShipIgnition");
                    RumbleManager.PlayShipIgnition();
                    RumbleManager.SetShipThrottleNormal();
                }
            }
        }

        // Store result into the lastTranslationalInput before further processing.
        // Idk why, it's what vanilla does.
        __instance._lastTranslationalInput = __result;

        // Scale in proportion to thrust limit.
        __result *= Mathf.Min(1f, __instance._rulesetDetector.GetThrustLimit() / __instance._thrusterModel.GetMaxTranslationalThrust());

        // This code runs in the landing cam when locked on.
        // It prevents you from orbiting too quickly when you're trying to land.
        if (__instance._limitOrbitSpeed && __instance._shipAlignment.IsAligning() && __result != Vector3.zero) {
            // Displacement from planet to ship
            var displacement = __instance._landingRF.GetOWRigidBody().GetWorldCenterOfMass() - __instance._shipBody.GetWorldCenterOfMass();

            // Velocity relative to the planet
            var vRel = __instance._shipBody.GetVelocity() - __instance._landingRF.GetVelocity();
            // Velocity perpendicular to planet displacement;
            var vPerp = vRel - Vector3.Project(vRel, displacement);

            // Thrust vector in world space
            // Assumes thrust is input*maxThrust in the ship's reference frame.
            // Not perfectly accurate because thrust is clamped in ThrusterModel.FireTranslationalThrusters
            //
            // Note: vanilla also multiplies by Quaternion.FromToRotation(-_shipBody.transform.up, displacement)
            // I don't think this is correct, but I'm not really sure. Either way, the inversion code in the if
            // block below does not undo the rotation in vanilla, so something was definitely incorrect.
            //
            // The question is if the alignment is applied before or after the thrust, and I'm not sure.
            // This assumes the thrust will be applied with the current alignment.
            // Either way, omitting the quaternion factor will not matter too much because the ship and
            // the planet are very nearly aligned, so the quaternion is close to 1.
            var thrust = __instance._shipBody.transform.TransformDirection(__result)
                * __instance._thrusterModel.GetMaxTranslationalThrust();

            // Components of thrust parallel and perpendicular to displacement.
            var thrustProj = Vector3.Project(thrust, displacement);
            var thrustPerp = thrust - thrustProj;

            // vPerp after a time step with the thrust applied.
            var newVPerp = vPerp + thrustPerp * Time.deltaTime;

            // Maximum allowed orbit speed
            var orbitSpeed = __instance._landingRF.GetOrbitSpeed(displacement.magnitude);
            // If our orbit speed is too great, clamp it.
            if (newVPerp.magnitude > orbitSpeed) {
                newVPerp = newVPerp.normalized * orbitSpeed;

                // Run all the calculations in reverse to figure out what
                // __result needs to be to achieve this vPerp.
                thrustPerp = (newVPerp - vPerp) / Time.deltaTime;
                thrust = thrustProj + thrustPerp;
                __result = __instance._shipBody.transform.InverseTransformDirection(thrust)
                    / __instance._thrusterModel.GetMaxTranslationalThrust();

                // __result may be too large if orbital velocity cannot be made small enough
                // in one time step. Vanilla uses this normalization code.
                // I don't think it matters because ThrusterModel already clamps thrust.
                
                //if (__result.sqrMagnitude > 1f) {
                //    __result.Normalize();
                //}
            }
        }
        return false;
    }
}
