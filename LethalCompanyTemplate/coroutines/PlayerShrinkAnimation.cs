using GameNetcodeStuff;
using LCShrinkRay.comp;
using LCShrinkRay.Config;
using LCShrinkRay.helper;
using LCShrinkRay.patches;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            routine.targetingUs = (affectedPlayer.playerClientId == PlayerHelper.currentPlayer().playerClientId);

            routine.StartCoroutine(routine.run(newSize, onComplete));
        }

        private IEnumerator run(float newSize, Action onComplete)
        {
            var playerTransform = targetPlayer.gameObject.GetComponent<Transform>();

            Transform armTransform = null, maskTransform = null;
            GrabbableObject heldItem = null;
            if(targetingUs)
            {
                armTransform = playerTransform.Find("ScavengerModel").Find("metarig").Find("ScavengerModelArmsOnly");
                maskTransform = GameObject.Find("ScavengerHelmet").GetComponent<Transform>();
                heldItem = PlayerHelper.HeldItem(targetPlayer);
            }

            float duration = 2f;
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
            var initialArmScale = armTransform.localScale;

            int count = 0;
            while (elapsedTime < duration && modificationType == ShrinkRay.ModificationType.Shrinking ? (currentSize > newSize) : (currentSize < newSize))
            {
                currentSize = (float)(directionalForce * Math.Sin((4 * elapsedTime / duration) + 0.81) + offset);

                var currentScale = new Vector3(currentSize, currentSize, currentSize);
                playerTransform.localScale = currentScale;

                if (targetingUs)
                {
                    maskTransform.localScale = CalcMaskScaleVec(currentSize);
                    maskTransform.localPosition = CalcMaskPosVec(currentSize);
                    var newArmScale = CalcArmScale(newSize);
                    armTransform.localScale = newArmScale;
                    if (heldItem != null)
                        ScreenBlockingGrabbablePatch.TransformItemRelativeTo(heldItem, currentSize, (initialArmScale - newArmScale) / 2);
                }

                elapsedTime += Time.deltaTime;

                count = count % 20 + 1;
                if(count == 1)
                {
                    adjustAllPlayerPitches(); // Adjust pitch & item every 20 frames
                    //if (targetingUs && heldItem != null)
                        //ScreenBlockingGrabbablePatch.CheckForGlassify(heldItem);
                }
                else
                    yield return null; // Wait for the next frame 
            }

            // Ensure final scale is set to the desired value
            var finalScale = new Vector3(newSize, newSize, newSize);
            playerTransform.localScale = new Vector3(newSize, newSize, newSize);
            if (targetingUs)
            {
                maskTransform.localScale = CalcMaskScaleVec(newSize);
                maskTransform.localPosition = CalcMaskPosVec(newSize);
                var newArmScale = CalcArmScale(newSize);
                armTransform.localScale = newArmScale;
                if (heldItem != null)
                {
                    ScreenBlockingGrabbablePatch.TransformItemRelativeTo(heldItem, currentSize, (initialArmScale - newArmScale) / 2);
                    ScreenBlockingGrabbablePatch.CheckForGlassify(heldItem);
                }
            }

            if (onComplete != null)
                onComplete();
        }
        private Vector3 CalcMaskPosVec(float scale)
        {
            Vector3 pos;
            float x = 0;
            float y = 0.00375f * scale + 0.05425f;
            float z = 0.005f * scale - 0.279f;
            pos = new Vector3(x, y, z);
            return pos;
        }

        private Vector3 CalcMaskScaleVec(float scale)
        {
            Vector3 pos;
            float x = 0.277f * scale + 0.2546f;
            float y = 0.2645f * scale + 0.267f;
            float z = 0.177f * scale + 0.3546f;
            pos = new Vector3(x, y, z);
            return pos;
        }

        private Vector3 CalcArmScale(float scale)
        {
            Vector3 pos;
            float x = 0.35f * scale  + 0.58f;
            float y = -0.0625f * scale + 1.0625f;
            float z = -0.125f * scale + 1.15f;
            pos = new Vector3(x, y, z);
            return pos;
        }

        private IEnumerator adjustAllPlayerPitches()
        {
            if (targetingUs) // Change pitch of every other player
            {
                foreach (var pcb in StartOfRound.Instance.allPlayerScripts.Where(p => p != null && p.isPlayerControlled && p.playerClientId != targetPlayer.playerClientId))
                    yield return adjustPlayerPitch(pcb);
            }
            else // Only need to change pitch of affected player
            {
                yield return adjustPlayerPitch(targetPlayer);
            }
        }

        private IEnumerator adjustPlayerPitch(PlayerControllerB pcb)
        {
            if (pcb.gameObject == null || pcb.gameObject.transform == null)
            {
                Plugin.log("SetPlayerPitch: Unable to get playerObj.transform", Plugin.LogType.Warning);
                yield break;
            }

            if (SoundManager.Instance == null)
            {
                Plugin.log("SetPlayerPitch: SoundManager is null", Plugin.LogType.Warning);
                yield break;
            }

            float playerScale = pcb.gameObject.transform.localScale.x;
            float intensity = (float)ModConfig.Instance.values.pitchDistortionIntensity;

            float modifiedPitch = (float)(-1f * intensity * (playerScale - PlayerHelper.currentPlayerScale()) + 1f);

            try
            {
                SoundManager.Instance.SetPlayerPitch(modifiedPitch, (int)pcb.playerClientId);
                Plugin.log("Pitch from player " + pcb.playerClientId + " adjusted to " + modifiedPitch + ". currentPlayerScale: " + PlayerHelper.currentPlayerScale() + " / playerScale: " + playerScale + " / intensity: " + intensity);
            }
            catch (NullReferenceException e)
            {
                Plugin.log("Hey! So, there's a null reference exception in SetPlayerPitch ... and here's why: " + e.ToString() + "\n" + e.StackTrace.ToString(), Plugin.LogType.Warning);
            }
            yield return null; // Wait for the next frame
        }
    }
}
