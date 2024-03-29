﻿using LittleCompany.components;
using System;
using System.Collections;
using UnityEngine;

namespace LittleCompany.coroutines
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
            if(!gameObj.TryGetComponent(out TargetScaling scaling))
                scaling = gameObj.AddComponent<TargetScaling>();

            if (scaling.SizeAt(desiredScale) == scaling.intendedScale)
                yield break;

            float duration = 2f;
            float elapsedTime = 0f;

            float c = scaling.IntendedSize;
            var direction = desiredScale < c ? -1f : 1f;
            float a = Mathf.Abs(c - desiredScale); // difference
            const float b = -0.5f;

            while (elapsedTime < duration)
            {
                // f(x) = -(a+1)(x/2)^2+bx+c [Shrinking] <-> (a+1)(x/2)^2-bx+c [Enlarging]
                var x = elapsedTime;
                var newScale = direction * (a + 1f) * Mathf.Pow(x / 2f, 2f) + (x * b * direction) + c;
                scaling.ScaleRelativeTo(newScale, default, true);

                elapsedTime += Time.deltaTime;
                yield return null; // Wait for the next frame
            }

            // Ensure final scale is set to the desired value
            scaling.ScaleRelativeTo(desiredScale, default, true);

            if (onComplete != null)
                onComplete();
        }
    }
}
