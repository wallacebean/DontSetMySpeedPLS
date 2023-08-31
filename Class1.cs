using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Logging;
using Reptile;
using Rewired;
using Reptile.Phone;
using System.Collections;
using System.Globalization;
using UnityEngine.AI;
using UnityEngine.Playables;
using System.Runtime.CompilerServices;

//edit air dash
//edit boost
//edit wall run ability
//tweak values where nessecary 
//GET FEEDBACK
//
//
//

namespace DontSetMySpeedPLS
{
    [BepInPlugin("us.wallace.plugins.BRC.DontSetMySpeedPLS", "DontSetMySpeedPLS Plug-In", "1.0.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource Log { get; private set; } = null;
        private void Awake()
        {
            Log = this.Logger;

            Logger.LogDebug("Patching effects settings...");

            var harmony = new Harmony("us.wallace.plugins.BRC.DontSetMySpeedPLS");
            harmony.PatchAll(typeof(UpdateTiltingPatch));
            harmony.PatchAll(typeof(GroundTrickAbilityOnStartAbilityPatch));
            harmony.PatchAll(typeof(GrindAbilityUpdateTricksPatch));
            harmony.PatchAll(typeof(GrindAbilityRewardTiltingPatch));
            harmony.PatchAll(typeof(GrindAbilityDoBoostTrickPatch));
            harmony.PatchAll(typeof(AirTrickAbilitySetupBoostTrickPatch));
            harmony.PatchAll(typeof(AirTrickAbilityOnStartAbilityPatch));
            harmony.PatchAll(typeof(PlayerJumpPatch));
            harmony.PatchAll(typeof(GroundTrickAbilityDoBoostTrickPatch));
            //harmony.PatchAll(typeof(WallrunLineAbilityOnStartAbilityTranspiler));
        }
    }

    class GroundTrickAbilityOnStartAbilityPatch
    {
        [HarmonyPatch(typeof(GroundTrickAbility), nameof(GroundTrickAbility.OnStartAbility))] //EPIC WIN (.. I LOVE OME HE DID THIS
        [HarmonyPrefix]
        public static void OnStartAbility_Prefix(GroundTrickAbility __instance)
        {

            float boost = __instance.p.boostSpeed; // Calculate the boost
            float L = -0.94f; // Set the maximum value of the sigmoid function
            float k = 0.4f; // Set the steepness of the curve
            float x0 = 0.2f; // Set the midpoint of the curve
            float sigmoid = (L / (1 + Mathf.Exp(-k * (__instance.p.GetForwardSpeed() - x0)))) + 1; // Calculate the sigmoid function
            __instance.hitEnemy = false;
            __instance.duration = 0.75f;
            if (__instance.p.moveStyle == global::Reptile.MoveStyle.SKATEBOARD || __instance.p.moveStyle == global::Reptile.MoveStyle.SPECIAL_SKATEBOARD)
            {
                __instance.duration = 0.8431f;
            }
            else if (__instance.p.moveStyle == global::Reptile.MoveStyle.BMX)
            {
                __instance.duration = 0.862f;
            }
            else if (__instance.p.moveStyle == global::Reptile.MoveStyle.INLINE)
            {
                __instance.duration = 0.75f;
            }
            __instance.defaultSpeed = __instance.p.maxMoveSpeed * 0.62f;
            __instance.curTrick = __instance.p.InputToTrickNumber();
            __instance.curTrickDir = __instance.trickingHitDir[__instance.curTrick];
            __instance.targetSpeed = __instance.defaultSpeed;
            __instance.rotateSpeed = 3f;
            __instance.acc = 35f;
            __instance.decc = __instance.p.stats.groundDecc / 2.5f;
            __instance.trickCount = 1;
            if (__instance.p.GetForwardSpeed() < __instance.defaultSpeed)
            {
                __instance.p.SetForwardSpeed(__instance.defaultSpeed);
            }
            if (__instance.p.GetForwardSpeed() > __instance.defaultSpeed)
            {
                __instance.p.SetForwardSpeed(__instance.p.GetForwardSpeed() /*speed*/ + boost * sigmoid); // Update the forward speed
            }
            __instance.wantToStop = (__instance.stopDecided = (__instance.reTrickFail = false));
            if (!__instance.p.IsComboing())
            {
                __instance.p.RefillComboTimeOut();
            }
            __instance.boostTrick = false;
            if (!__instance.p.isAI && __instance.p.CheckBoostTrick())
            {
                __instance.DoBoostTrick();
            }
            else
            {
                __instance.p.PlayAnim(__instance.groundTrickHashes[__instance.curTrick], true, false, 0f);
            }
            __instance.p.hitboxLeftLeg.SetActive(false);
            __instance.p.hitboxRightLeg.SetActive(false);
            __instance.p.hitboxUpperBody.SetActive(false);
            if (!__instance.p.isAI)
            {
                __instance.p.StartClosestCheckEnemyForAutoAim();
            }

        }
    }


    class GrindAbilityUpdateTricksPatch
    {
        [HarmonyPatch(typeof(GrindAbility), nameof(GrindAbility.UpdateTricks))] //EPIC WIN (.. I LOVE OME HE DID THIS
        [HarmonyPrefix]
        public static void UpdateTricks_Prefix(GrindAbility __instance)
        {
            float boost = __instance.p.boostSpeed; // Calculate the boost
            float L = -0.94f; // Set the maximum value of the sigmoid function
            float k = 0.4f; // Set the steepness of the curve
            float x0 = 0.2f; // Set the midpoint of the curve
            float sigmoid = (L / (1 + Mathf.Exp(-k * (__instance.p.GetForwardSpeed() - x0)))) + 1; // Calculate the sigmoid function


            __instance.p.SetForwardSpeed(__instance.p.GetForwardSpeed() /*speed*/ + boost * sigmoid); // Update the forward speed


            if (__instance.trickBuffered)
            {
                if (__instance.trickTimer <= 0f)
                {
                    __instance.StartGrindTrick(false, false);
                }
            }
            else if (__instance.p.AnyTrickInput() && !__instance.braking)
            {
                if (__instance.trickTimer > __instance.reTrickMargin)
                {
                    __instance.reTrickFail = true;
                }
                else if (!__instance.reTrickFail)
                {
                    __instance.StartGrindTrick(false, false);
                }
                else if (__instance.trickTimer <= 0f)
                {
                    __instance.StartGrindTrick(false, false);
                }
            }
            if (__instance.trickTimer > 0f)
            {
                __instance.trickTimer -= global::Reptile.Core.dt;
                if (__instance.curTrickBoost)
                {
                    if (__instance.trickTimer > __instance.curTrickDuration - 0.35f && !__instance.p.isAI)
                    {
                        global::Reptile.Core.Instance.GameInput.SetVibrationOnCurrentController(0.19f, 0.19f, 0.1f, 0);
                    }
                }
                else if (!__instance.p.isAI && __instance.trickTimer > __instance.curTrickDuration - 0.1f)
                {
                    if (__instance.p.CheckBoostTrick())
                    {
                        __instance.DoBoostTrick();
                    }
                }
                else if (!__instance.p.didAbilityTrick)
                {
                    __instance.p.DoTrick(global::Reptile.Player.TrickType.GRIND, __instance.trickNames[__instance.curTrick], __instance.curTrick);
                }
                if (__instance.trickTimer < 0f)
                {
                    __instance.p.hitbox.SetActive(false);
                }
            }
            int curAnim = __instance.p.curAnim;
            bool flag = false;
            int num = 3;
            for (int i = 0; i < num; i++)
            {
                flag |= (curAnim == __instance.grindTrickHashes[i]);
            }
            if (flag)
            {
                __instance.p.targetMovement = global::Reptile.Player.MovementType.NONE;
            }
        }
    }

    class WallrunLineAbilityOnStartAbilityTranspiler
    {
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(WallrunLineAbility), "OnStartAbility", MethodType.Enumerator)]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var getResultNumber = AccessTools.Method(typeof(MovementMotor), "HaveCollision");
            var found = false;
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Callvirt && instruction.operand == (object)getResultNumber)
                {
                    //yield return new CodeInstruction(OpCodes.Call, m_MyExtraMethod);
                    found = true;
                }
                yield return instruction;
            }
            if (found is false)
                throw new ArgumentException("Cannot find <Stdfld someField> in OriginalType.OriginalMethod");
        }
    }


    class GrindAbilityRewardTiltingPatch
    {
        [HarmonyPatch(typeof(GrindAbility), nameof(GrindAbility.RewardTilting))] //EPIC WIN (.. I LOVE OME HE DID THIS
        [HarmonyPrefix]
        public static void RewardTilting_Prefix(GrindAbility __instance, global::UnityEngine.Vector3 rightDir, global::UnityEngine.Vector3 nextLineDir)
        {
            float cornerboost = __instance.cornerBoost; // Calculate the boost
            float L = -0.94f; // Set the maximum value of the sigmoid function
            float k = 0.4f; // Set the steepness of the curve
            float x0 = 9f; // Set the midpoint of the curve
            float sigmoid = (L / (1 + Mathf.Exp(-k * (__instance.p.GetForwardSpeed() - x0)))) + 1; // Calculate the sigmoid function
            if (!__instance.grindLine.cornerBoost)
            {
                return;
            }
            float num = global::UnityEngine.Vector3.Dot(rightDir, global::UnityEngine.Vector3.ProjectOnPlane(nextLineDir, global::UnityEngine.Vector3.up).normalized);
            global::Reptile.Side side = global::Reptile.Side.NONE;
            if (__instance.grindTiltBuffer.x < -0.25f)
            {
                side = global::Reptile.Side.LEFT;
            }
            else if (__instance.grindTiltBuffer.x > 0.25f)
            {
                side = global::Reptile.Side.RIGHT;
            }
            global::Reptile.Side side2 = global::Reptile.Side.NONE;
            if (num < -0.02f)
            {
                side2 = global::Reptile.Side.LEFT;
            }
            else if (num > 0.02f)
            {
                side2 = global::Reptile.Side.RIGHT;
            }
            if (side2 != global::Reptile.Side.NONE)
            {
                __instance.softCornerBoost = false;
            }
            bool flag = global::UnityEngine.Mathf.Abs(num) > 0.3f;
            if (side != global::Reptile.Side.NONE && side == side2)
            {
                if (flag && __instance.lastPath.hardCornerBoostsAllowed)
                {
                    __instance.p.StartScreenShake(global::Reptile.ScreenShakeType.LIGHT, 0.2f, false);
                    __instance.p.AudioManager.PlaySfxGameplay(global::Reptile.SfxCollectionID.GenericMovementSfx, global::Reptile.AudioClipID.singleBoost, __instance.p.playerOneShotAudioSource, 0f);
                    __instance.p.ringParticles.Emit(1);

                    __instance.p.SetForwardSpeed(__instance.p.GetForwardSpeed() /*speed*/ + cornerboost * sigmoid); // Update the forward speed
                    __instance.p.HardCornerGrindLine(__instance.nextNode);
                    return;
                }
                if (__instance.lastPath.softCornerBoostsAllowed)
                {
                    __instance.p.SetForwardSpeed(__instance.p.GetForwardSpeed() /*speed*/ + (cornerboost * sigmoid) / 5); // Update the forward speed
                    __instance.p.DoTrick(global::Reptile.Player.TrickType.SOFT_CORNER, "Corner", 0);

                }
            }
        }
    }
    class GrindAbilityDoBoostTrickPatch
    {
        [HarmonyPatch(typeof(GrindAbility), nameof(GrindAbility.DoBoostTrick))] //EPIC WIN (.. I LOVE OME HE DID THIS
        [HarmonyPrefix]
        public static void DoBoostTrick_Prefix(GrindAbility __instance)
        {
            float boost = __instance.p.boostSpeed; // Calculate the boost
            float L = -0.93f; // Set the maximum value of the sigmoid function
            float k = 0.3f; // Set the steepness of the curve
            float x0 = 5f; // Set the midpoint of the curve
            float sigmoid = (L / (1 + Mathf.Exp(-k * (__instance.p.GetForwardSpeed() - x0)))) + 1; // Calculate the sigmoid function
            __instance.curTrickBoost = true;
            __instance.p.ringParticles.Emit(1);
            __instance.trickTimer = (__instance.curTrickDuration = __instance.trickStandardDuration * 1.8f);
            if (!__instance.p.isAI)
            {
                __instance.p.SetForwardSpeed(__instance.p.GetForwardSpeed() /*speed*/ + boost * sigmoid); // Update the forward speed
                
            }
            __instance.p.AddBoostCharge(-__instance.p.boostTrickCost);
            __instance.p.DoTrick(global::Reptile.Player.TrickType.GRIND_BOOST, __instance.startTrickNames[3 + __instance.curTrick], __instance.curTrick);
            __instance.p.PlayVoice(global::Reptile.AudioClipID.VoiceBoostTrick, global::Reptile.VoicePriority.BOOST_TRICK, true);
            __instance.p.PlayAnim(__instance.grindBoostTrickHashes[__instance.curTrick], true, false, 0f);
        }
    }


    class GroundTrickAbilityDoBoostTrickPatch
    {
        [HarmonyPatch(typeof(GroundTrickAbility), nameof(GroundTrickAbility.DoBoostTrick))] //EPIC WIN (.. I LOVE OME HE DID THIS
        [HarmonyPrefix]
        public static bool DoBoostTrick_Prefix(GroundTrickAbility __instance)
        {
            float boost = __instance.p.boostSpeed; // Calculate the boost
            float L = -0.93f; // Set the maximum value of the sigmoid function
            float k = 0.3f; // Set the steepness of the curve
            float x0 = 5f; // Set the midpoint of the curve
            float sigmoid = (L / (1 + Mathf.Exp(-k * (__instance.p.GetForwardSpeed() - x0)))) + 1f; // Calculate the sigmoid function
            __instance.p.PlayAnim(__instance.groundBoostTrickHashes[__instance.curTrick], true, false, 0f);
            __instance.boostTrick = true;
            __instance.p.ringParticles.Emit(1);
            __instance.duration *= 1.8f;
            if (!__instance.p.isAI)
            {
                __instance.p.SetForwardSpeed(__instance.p.GetForwardSpeed() /*speed*/ + boost * sigmoid); // Update the forward speed
            }
            __instance.p.DoTrick(global::Reptile.Player.TrickType.GROUND_BOOST, __instance.GetTrickName(__instance.curTrick, true), __instance.curTrick);
            __instance.p.PlayVoice(global::Reptile.AudioClipID.VoiceBoostTrick, global::Reptile.VoicePriority.BOOST_TRICK, true);
            __instance.p.AddBoostCharge(-__instance.p.boostTrickCost);
            return false;
        }
    }


    class AirTrickAbilityOnStartAbilityPatch
    {
        [HarmonyPatch(typeof(AirTrickAbility), nameof(AirTrickAbility.OnStartAbility))] //EPIC WIN (.. I LOVE OME HE DID THIS
        [HarmonyPrefix]
        public static void OnStartAbility_Prefix(AirTrickAbility __instance)
        {
            float boost = __instance.p.boostSpeed; // Calculate the boost
            float L = -0.94f; // Set the maximum value of the sigmoid function
            float k = 0.4f; // Set the steepness of the curve
            float x0 = 0.2f; // Set the midpoint of the curve
            float sigmoid = (L / (1 + Mathf.Exp(-k * (__instance.p.GetForwardSpeed() - x0)))) + 1; // Calculate the sigmoid function

            __instance.p.SetForwardSpeed(__instance.p.GetForwardSpeed() /*speed*/ + boost * sigmoid); // Update the forward speed

            __instance.hitEnemy = false;
            __instance.p.SetDustEmission(0);
            __instance.curTrick = __instance.p.InputToTrickNumber();
            __instance.bufferSwitchStyle = false;
            __instance.targetSpeed = -1f;
            __instance.rotateSpeed = -1f;
            __instance.decc = -1f;
            __instance.trickType = global::Reptile.AirTrickAbility.TrickType.NORMAL_TRICK;
            __instance.duration = __instance.trickingTrickDuration;
            if (__instance.p.moveStyle == global::Reptile.MoveStyle.SKATEBOARD || __instance.p.moveStyle == global::Reptile.MoveStyle.SPECIAL_SKATEBOARD)
            {
                __instance.duration = __instance.skateboardTrickDuration;
            }
            else if (__instance.p.moveStyle == global::Reptile.MoveStyle.BMX)
            {
                __instance.duration = __instance.bmxTrickDuration;
            }
            else if (__instance.p.moveStyle == global::Reptile.MoveStyle.INLINE)
            {
                __instance.duration = __instance.inlineTrickDuration;
            }
            if (__instance.p.CheckBoostTrick())
            {
                __instance.SetupBoostTrick();
            }
            else
            {
                __instance.p.PlayAnim(__instance.airTrickHashes[__instance.curTrick], true, false, 0f);
            }
            __instance.p.hitboxLeftLeg.SetActive(false);
            __instance.p.hitboxRightLeg.SetActive(false);
            __instance.p.hitboxUpperBody.SetActive(false);
        }
    }

    class AirTrickAbilitySetupBoostTrickPatch
    {
        [HarmonyPatch(typeof(AirTrickAbility), nameof(AirTrickAbility.SetupBoostTrick))] //EPIC WIN (.. I LOVE OME HE DID THIS
        [HarmonyPrefix]
        public static bool SetupBoostTrick_Prefix(AirTrickAbility __instance)
        {
            float boost = __instance.p.boostSpeed; // Calculate the boost
            float L = -0.93f; // Set the maximum value of the sigmoid function
            float k = 0.3f; // Set the steepness of the curve
            float x0 = 5f; // Set the midpoint of the curve
            float sigmoid = (L / (1 + Mathf.Exp(-k * (__instance.p.GetForwardSpeed() - x0)))) + 1; // Calculate the sigmoid function
            __instance.p.PlayAnim(__instance.airBoostTrickHashes[__instance.curTrick], true, false, 0f);
            __instance.p.PlayVoice(global::Reptile.AudioClipID.VoiceBoostTrick, global::Reptile.VoicePriority.BOOST_TRICK, true);
            __instance.p.ringParticles.Emit(1);
            __instance.trickType = global::Reptile.AirTrickAbility.TrickType.BOOST_TRICK;
            __instance.duration *= 1.5f;
            if (!__instance.p.isAI)
            {
                __instance.p.SetForwardSpeed(__instance.p.GetForwardSpeed() /*speed*/ + boost * sigmoid); // Update the forward speed
            }
            __instance.p.AddBoostCharge(-__instance.p.boostTrickCost);
            return false;
        }
    }

    class UpdateTiltingPatch
    {
        [HarmonyPatch(typeof(GrindAbility), nameof(GrindAbility.UpdateTilting))]
        [HarmonyPrefix]
        public static bool UpdateTilting_Prefix(GrindAbility __instance)
        {
            float num = __instance.p.moveInputPlain.x;
            float maxDelta = 0.18f;
            if (global::UnityEngine.Vector3.Dot(__instance.p.tf.up, global::UnityEngine.Vector3.down) > 0.5f)
            {
                num *= -1f;
            }
            if (num > 0f)
            {
                __instance.grindTilt.x = global::Reptile.Utility.Lerp(__instance.grindTilt.x, num, 0.4f, maxDelta);
            }
            else if (num < 0f)
            {
                __instance.grindTilt.x = global::Reptile.Utility.Lerp(__instance.grindTilt.x, num, 0.4f, maxDelta);
            }
            else if (__instance.grindTilt.x > 0f)
            {
                __instance.grindTilt.x = global::Reptile.Utility.Lerp(__instance.grindTilt.x, 0f, 0.3f, maxDelta);
            }
            else if (__instance.grindTilt.x < 0f)
            {
                __instance.grindTilt.x = global::Reptile.Utility.Lerp(__instance.grindTilt.x, 0f, 0.3f, maxDelta);
            }
            if (global::UnityEngine.Mathf.Abs(__instance.grindTilt.x) > 0.25f)
            {
                __instance.grindTiltBuffer.x = __instance.grindTilt.x;
                __instance.grindTiltBufferTimer = 0.1f;
            }
            if (__instance.grindTiltBufferTimer > 0f)
            {
                __instance.grindTiltBufferTimer -= global::Reptile.Core.dt;
                if (__instance.grindTiltBufferTimer <= 0f)
                {
                    __instance.grindTiltBuffer.x = 0f;
                }
            }
            __instance.p.anim.SetFloat(__instance.grindDirectionHash, __instance.grindTilt.x * __instance.grindTiltAnimMultiplyer);
            return false;
        }
    }

    class PlayerJumpPatch
    {
        [HarmonyPatch(typeof(Player), nameof(Player.Jump))] //EPIC WIN (.. I LOVE OME HE DID THIS
        [HarmonyPrefix]
        public static void Jump_Prefix(Player __instance)
        {
            float boost = __instance.boostSpeed; // Calculate the boost
            float L = -0.93f; // Set the maximum value of the sigmoid function
            float k = 0.3f; // Set the steepness of the curve
            float x0 = 5f; // Set the midpoint of the curve
            float sigmoid = (L / (1 + Mathf.Exp(-k * (__instance.GetForwardSpeed() - x0)))) + 1; // Calculate the sigmoid function
            __instance.audioManager.PlayVoice(ref __instance.currentVoicePriority, __instance.character, global::Reptile.AudioClipID.VoiceJump, __instance.playerGameplayVoicesAudioSource, global::Reptile.VoicePriority.MOVEMENT);
            __instance.PlayAnim(__instance.jumpHash, false, false, -1f);
            float num = 1f;
            if (__instance.targetMovement == global::Reptile.Player.MovementType.RUNNING && __instance.IsGrounded() && __instance.motor.groundCollider.gameObject.layer != 23 && global::UnityEngine.Vector3.Dot(global::UnityEngine.Vector3.ProjectOnPlane(__instance.motor.groundNormal, global::UnityEngine.Vector3.up).normalized, __instance.dir) < -0.5f)
            {
                num = 1f + global::UnityEngine.Mathf.Min(__instance.motor.groundAngle / 90f, 0.3f);
            }
            __instance.ForceUnground(true);
            float num2 = 0f;
            if (__instance.timeSinceGrinding <= __instance.JumpPostGroundingGraceTime)
            {
                num2 = __instance.bonusJumpSpeedGrind;
            }
            else if (__instance.timeSinceWallrunning <= __instance.JumpPostGroundingGraceTime)
            {
                num2 = __instance.bonusJumpSpeedWallrun;
            }
            float num3 = __instance.jumpSpeed * num + num2;
            if (num2 != 0f && __instance.slideButtonHeld)
            {
                num3 *= __instance.abilityShorthopFactor;
                __instance.maintainSpeedJump = true;
            }
            else
            {
                __instance.maintainSpeedJump = false;
            }
            if (__instance.onLauncher)
            {
                if (!__instance.onLauncher.parent.gameObject.name.Contains("Super"))
                {
                    __instance.motor.SetVelocityYOneTime(__instance.jumpSpeedLauncher);
                }
                else
                {
                    __instance.motor.SetVelocityYOneTime(__instance.jumpSpeedLauncher * 1.4f);
                }
                if (__instance.targetMovement == global::Reptile.Player.MovementType.RUNNING && global::UnityEngine.Vector3.Dot(__instance.dir, __instance.onLauncher.back()) > 0.7f && !__instance.onLauncher.parent.gameObject.name.Contains("flat"))
                {
                    __instance.SetForwardSpeed(__instance.GetForwardSpeed() /*speed*/ + (boost * sigmoid) / 5); // Update the forward speed
                }
                __instance.audioManager.PlaySfxGameplay(global::Reptile.SfxCollectionID.GenericMovementSfx, global::Reptile.AudioClipID.launcher_woosh, __instance.playerOneShotAudioSource, 0f);
                __instance.DoHighJumpEffects(__instance.motor.groundNormalVisual * -1f);
            }
            else
            {
                __instance.DoJumpEffects(__instance.motor.groundNormalVisual * -1f);
                __instance.motor.SetVelocityYOneTime(num3);
                __instance.isJumping = true;
            }
            __instance.jumpRequested = false;
            __instance.jumpConsumed = true;
            __instance.jumpedThisFrame = true;
            __instance.timeSinceLastJump = 0f;
            if (__instance.ability != null)
            {
                __instance.ability.OnJump();
                if (__instance.ability != null && __instance.onLauncher && __instance.ability.autoAirTrickFromLauncher)
                {
                    __instance.ActivateAbility(__instance.airTrickAbility);
                    return;
                }
            }
            else if (__instance.onLauncher)
            {
                __instance.ActivateAbility(__instance.airTrickAbility);
            }
        }
    }








    class ClassMethodPatch
    {
        [HarmonyPatch(typeof(AirTrickAbility), nameof(AirTrickAbility.SetupBoostTrick))] //EPIC WIN (.. I LOVE OME HE DID THIS
        [HarmonyPrefix]
        public static void SetupBoostTrick_Prefix(AirTrickAbility __instance)
        {

        }
    }


}


