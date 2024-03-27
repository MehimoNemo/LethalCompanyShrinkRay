using GameNetcodeStuff;
using LittleCompany.components;
using LittleCompany.helper;
using LittleCompany.patches;
using System;
using System.Collections;
using UnityEngine;

namespace LittleCompany.coroutines
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

            GrabbableObject heldItem = null;
            Vector3 initialArmScale = Vector3.one;
            float currentSize = PlayerInfo.SizeOf(targetPlayer);

            if (targetingUs)
                heldItem = PlayerInfo.HeldItem(targetPlayer);

            var modifiedPlayerModel = false; // Modified by other mods -> only scale transform
            var playerTransform = PlayerInfo.SpineOf(targetPlayer);
            if (playerTransform == null)
            {
                modifiedPlayerModel = true;
                playerTransform = targetPlayer.gameObject.transform;
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
                currentSize = direction * (a + 1f) * Mathf.Pow(x / 2f, 2f) + (x * b * direction) + c;

                var currentScale = Vector3.one * currentSize;
                playerTransform.localScale = currentScale;
                if (!modifiedPlayerModel)
                    playerTransform.localPosition = new Vector3(0f, currentSize - 1f, 0f);

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
            playerTransform.localScale = Vector3.one * newSize;
            if (!modifiedPlayerModel)
                playerTransform.localPosition = new Vector3(0f, newSize - 1f, 0f);
            if (targetingUs)
            {
                PlayerInfo.ScaleLocalPlayerBodyParts();
                if (heldItem != null)
                {
                    ScreenBlockingGrabbablePatch.TransformItemRelativeTo(heldItem, currentSize, (initialArmScale - PlayerInfo.CalcLocalArmScale()) / 2);
                    ScreenBlockingGrabbablePatch.CheckForGlassify(heldItem);
                }
                if (newSize != 1f)
                    PlayerMultiplierPatch.Modify(newSize);
                else
                    PlayerMultiplierPatch.Reset();
            }

            if (onComplete != null)
                onComplete();
        }
    }
}
