using LCShrinkRay.comp;
using System;
using System.Collections;
using UnityEngine;

namespace LCShrinkRay.coroutines
{
    internal class ObjectShrinkAnimation : MonoBehaviour // todo: rename to ObjectChangeSizeAnimation
    {
        public GameObject gameObj { get; private set; }

        public static void StartRoutine(GameObject gameObj, float newSize, Action onComplete = null)
        {
            var routine = gameObj.AddComponent<ObjectShrinkAnimation>();
            routine.gameObj = gameObj;
            routine.StartCoroutine(routine.Run(newSize, onComplete));
        }

        private IEnumerator Run(float desiredScale, Action onComplete)
        {
            Plugin.Log("ENTERING COROUTINE OBJECT SHRINK", Plugin.LogType.Warning);
            Plugin.Log("gObject: " + gameObj, Plugin.LogType.Warning);

            if(!gameObj.TryGetComponent(out TargetScaling scaling))
                scaling = gameObj.AddComponent<TargetScaling>();

            if (scaling.SizeAt(desiredScale) == scaling.originalScale)
                yield break;

            float duration = 2f;
            float elapsedTime = 0f;

            float c = scaling.CurrentScale;
            var direction = desiredScale < c ? -1f : 1f;
            float a = Mathf.Abs(c - desiredScale); // difference
            const float b = -0.5f;

            while (elapsedTime < duration)
            {
                // f(x) = -(a+1)(x/2)^2+bx+c [Shrinking] <-> (a+1)(x/2)^2-bx+c [Enlarging]
                var x = elapsedTime;
                var newScale = direction * (a + 1f) * Mathf.Pow(x / 2f, 2f) + (x * b * direction) + 1f;
                scaling.ScaleRelativeTo(newScale);

                elapsedTime += Time.deltaTime;
                yield return null; // Wait for the next frame
            }

            // Ensure final scale is set to the desired value
            scaling.ScaleRelativeTo(desiredScale);

            if (desiredScale == 1f)
                Destroy(scaling);

            if (onComplete != null)
                onComplete();
        }
    }
}
