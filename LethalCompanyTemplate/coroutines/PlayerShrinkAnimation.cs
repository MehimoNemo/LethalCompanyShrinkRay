using GameNetcodeStuff;
using LCShrinkRay.comp;
using LCShrinkRay.Config;
using LCShrinkRay.helper;
using LCShrinkRay.patches;
using System;
using System.Collections;
using UnityEngine;

namespace LCShrinkRay.coroutines
{
    internal class PlayerShrinkAnimation : MonoBehaviour
    {
        public PlayerControllerB targetPlayer { get; private set; }
        public bool targetingUs {  get; private set; }

        public static void StartRoutine(PlayerControllerB affectedPlayer, float newSize, Action onComplete = null)
        {
            var routine = affectedPlayer.gameObject.AddComponent<PlayerShrinkAnimation>();
            routine.targetPlayer = affectedPlayer;
            routine.targetingUs = (affectedPlayer.playerClientId == PlayerInfo.CurrentPlayerID);

            routine.StartCoroutine(routine.Run(newSize, onComplete));
        }

        private IEnumerator Run(float newSize, Action onComplete)
        {
            if (targetPlayer == null || targetPlayer.gameObject == null)
            {
                Plugin.Log("Attempting to shrink non existing player", Plugin.LogType.Warning);
                yield break;
            }

            var playerTransform = targetPlayer.gameObject.transform;

            Transform armTransform = null, maskTransform = null;
            GrabbableObject heldItem = null;
            Vector3 initialArmScale = Vector3.one;
            if(targetingUs)
            {
                armTransform = playerTransform.Find("ScavengerModel")?.Find("metarig")?.Find("ScavengerModelArmsOnly");
                if (armTransform == null)
                    Plugin.Log("Ray was targeting us, but we don't have arms??", Plugin.LogType.Warning);

                maskTransform = GameObject.Find("ScavengerHelmet")?.GetComponent<Transform>();
                if (maskTransform == null)
                    Plugin.Log("Ray was targeting us, but we don't have a helmet??", Plugin.LogType.Warning);

                heldItem = PlayerInfo.HeldItem(targetPlayer);
                initialArmScale = armTransform.localScale;
            }

            float elapsedTime = 0f;
            float currentSize = targetPlayer.gameObject.transform.localScale.x;

            var modificationType = newSize < currentSize ? ShrinkRay.ModificationType.Shrinking : ShrinkRay.ModificationType.Enlarging;
            float directionalForce, offset;
            if (modificationType == ShrinkRay.ModificationType.Shrinking)
            {
                directionalForce = 0.58f;
                offset = currentSize - 0.42f;
            }
            else
            {
                directionalForce = -0.58f;
                offset = currentSize + 0.42f;
            }

            int count = 0;
            while (elapsedTime < ShrinkRayFX.beamDuration && modificationType == ShrinkRay.ModificationType.Shrinking ? (currentSize > newSize) : (currentSize < newSize))
            {
                currentSize = (float)(directionalForce * Math.Sin((4 * elapsedTime / ShrinkRayFX.beamDuration) + 0.81) + offset);

                var currentScale = new Vector3(currentSize, currentSize, currentSize);
                playerTransform.localScale = currentScale;

                if (targetingUs)
                {
                    if (maskTransform != null)
                    {
                        maskTransform.localScale = PlayerInfo.CalcMaskScaleVec(currentSize);
                        maskTransform.localPosition = PlayerInfo.CalcMaskPosVec(currentSize);
                    }
                    var newArmScale = PlayerInfo.CalcArmScale(currentSize);
                    if(armTransform != null)
                        armTransform.localScale = newArmScale;
                    if (heldItem != null)
                        ScreenBlockingGrabbablePatch.TransformItemRelativeTo(heldItem, currentSize, (initialArmScale - newArmScale) / 2);
                }

                elapsedTime += Time.deltaTime;

                count = count % 20 + 1;
                if(count == 1)
                {
                    yield return AdjustAllPlayerPitches(); // Adjust pitch & item every 20 frames
                    //if (targetingUs && heldItem != null)
                        //ScreenBlockingGrabbablePatch.CheckForGlassify(heldItem);
                }
                else
                    yield return null; // Wait for the next frame 
            }

            // Ensure final scale is set to the desired value
            playerTransform.localScale = new Vector3(newSize, newSize, newSize);
            if (targetingUs)
            {
                if (maskTransform != null)
                {
                    maskTransform.localScale = PlayerInfo.CalcMaskScaleVec(newSize);
                    maskTransform.localPosition = PlayerInfo.CalcMaskPosVec(newSize);
                }
                var newArmScale = PlayerInfo.CalcArmScale(newSize);
                if (armTransform != null)
                    armTransform.localScale = newArmScale;
                if (heldItem != null)
                {
                    ScreenBlockingGrabbablePatch.TransformItemRelativeTo(heldItem, currentSize, (initialArmScale - newArmScale) / 2);
                    ScreenBlockingGrabbablePatch.CheckForGlassify(heldItem);
                }
                if (newSize != 1f)
                    PlayerModificationPatch.Modify(newSize);
                else
                    PlayerModificationPatch.Reset();
            }

            yield return AdjustAllPlayerPitches(); // Adjust pitch & item every 20 frames

            if (onComplete != null)
                onComplete();
        }

        private IEnumerator AdjustAllPlayerPitches()
        {
            if (!targetingUs) // Only need to change pitch of affected player
            {
                yield return AdjustPlayerPitch(targetPlayer);
                yield break;
            }

            // Change pitch of every other player
            foreach (var pcb in StartOfRound.Instance.allPlayerScripts)
            {
                if (pcb != null && pcb.isPlayerControlled && pcb.playerClientId != targetPlayer.playerClientId)
                    yield return AdjustPlayerPitch(pcb);
            }
        }

        private IEnumerator AdjustPlayerPitch(PlayerControllerB pcb)
        {
            if (pcb == null || pcb.gameObject == null)
            {
                Plugin.Log("SetPlayerPitch: Unable to get player gameObject", Plugin.LogType.Warning);
                yield break;
            }

            if (SoundManager.Instance == null)
            {
                Plugin.Log("SetPlayerPitch: SoundManager is null", Plugin.LogType.Warning);
                yield break;
            }

            float playerScale = pcb.gameObject.transform.localScale.x;
            float intensity = (float)ModConfig.Instance.values.pitchDistortionIntensity;

            float modifiedPitch = (float)(-1f * intensity * (playerScale - PlayerInfo.CurrentPlayerScale) + 1f);

            try
            {
                SoundManager.Instance.SetPlayerPitch(modifiedPitch, (int)pcb.playerClientId);
                Plugin.Log("Pitch from player " + pcb.playerClientId + " adjusted to " + modifiedPitch + ". currentPlayerScale: " + PlayerInfo.CurrentPlayerScale + " / playerScale: " + playerScale + " / intensity: " + intensity);
            }
            catch (NullReferenceException e)
            {
                Plugin.Log("Hey! So, there's a null reference exception in SetPlayerPitch ... and here's why: " + e.ToString() + "\n" + e.StackTrace.ToString(), Plugin.LogType.Warning);
            }
            yield return null; // Wait for the next frame
        }
    }
}
