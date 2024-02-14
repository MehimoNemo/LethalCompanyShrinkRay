using GameNetcodeStuff;
using LCShrinkRay.comp;
using System;
using System.Collections;
using UnityEngine;

namespace LCShrinkRay.coroutines
{
    internal class PlayerThrowAnimation : MonoBehaviour
    {
        public static void StartRoutine(PlayerControllerB targetPlayer, Vector3 direction, float force, Action onComplete = null)
        {
            if (targetPlayer?.gameObject == null )
            {
                Plugin.Log("Attempted to throw player, but gameObject was null.", Plugin.LogType.Error);
                return;
            }

            var routine = targetPlayer.gameObject.AddComponent<PlayerThrowAnimation>();
            routine.StartCoroutine(routine.Run(targetPlayer, direction, force, onComplete));
        }

        private IEnumerator Run(PlayerControllerB targetPlayer, Vector3 direction, float force, Action onComplete = null)
        {
            Plugin.Log("ThrowGrabbedPlayer");
            float time = 0f, duration = 0.5f;

            direction.y = Mathf.Max(direction.y, -1f) + 1f; // Don't throw backwards
            direction.y = Mathf.Min(direction.y, 1.8f); // Don't throw them high enough to take damage.. that's evil!

            Vector3 startForce = direction * force;

            while (time < duration)
            {
                var sinusProgress = Mathf.Lerp(Mathf.PI / 2f, 0f, time / duration);
                targetPlayer.externalForces = startForce * sinusProgress * direction.y; // direction.y to throw further when looking up
                //Plugin.Log("ExternalForces: " + targetPlayer.externalForces);

                time += Time.deltaTime;
                yield return null;
            }
            targetPlayer.externalForces = Vector3.zero;

            if (onComplete != null)
                onComplete();
        }
    }
}
