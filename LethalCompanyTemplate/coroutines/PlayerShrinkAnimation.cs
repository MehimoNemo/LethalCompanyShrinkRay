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
            /*var spine = PlayerInfo.SpineOf(targetPlayer);
            if (spine != null)
                spine.SetParent(playerTransform);*/

            GrabbableObject heldItem = null;
            Vector3 initialArmScale = Vector3.one;
            float currentSize = targetPlayer.gameObject.transform.localScale.x;

            if (targetingUs)
            {
                heldItem = PlayerInfo.HeldItem(targetPlayer);
                initialArmScale = PlayerInfo.CalcLocalArmScale();
            }

            float elapsedTime = 0f;

            var direction = newSize < currentSize ? -1 : 1;
            float a = Mathf.Abs(currentSize - newSize); // difference
            const float b = -0.5f;
            float c = currentSize;

            while (elapsedTime < ShrinkRayFX.beamDuration)
            {
                // f(x) = -(a+1)(x/2)^2+bx+c [Shrinking] <-> (a+1)(x/2)^2-bx+c [Enlarging]
                var x = elapsedTime;
                currentSize = direction * (a + 1f) * Mathf.Pow(x / 2, 2) + (x * b * direction) + c;

                var currentScale = new Vector3(currentSize, currentSize, currentSize);
                playerTransform.localScale = currentScale;

                if (targetingUs)
                {
                    PlayerInfo.ScaleLocalPlayerBodyParts();
                    if (heldItem != null)
                        ScreenBlockingGrabbablePatch.TransformItemRelativeTo(heldItem, currentSize, (initialArmScale - PlayerInfo.CalcLocalArmScale()) / 2);
                }

                elapsedTime += Time.deltaTime;

                yield return null; // Wait for the next frame 
            }

            // Ensure final scale is set to the desired value
            playerTransform.localScale = new Vector3(newSize, newSize, newSize);
            if (targetingUs)
            {
                PlayerInfo.ScaleLocalPlayerBodyParts();
                if (heldItem != null)
                {
                    ScreenBlockingGrabbablePatch.TransformItemRelativeTo(heldItem, currentSize, (initialArmScale - PlayerInfo.CalcLocalArmScale()) / 2);
                    ScreenBlockingGrabbablePatch.CheckForGlassify(heldItem);
                }
                if (newSize != 1f)
                    PlayerModificationPatch.Modify(newSize);
                else
                    PlayerModificationPatch.Reset();
            }

            if (onComplete != null)
                onComplete();
        }
    }
}
