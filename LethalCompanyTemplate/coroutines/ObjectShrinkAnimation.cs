using LCShrinkRay.comp;
using System;
using System.Collections;
using UnityEngine;

namespace LCShrinkRay.coroutines
{
    internal class ObjectShrinkAnimation : MonoBehaviour // todo: rename to ObjectChangeSizeAnimation
    {
        public GameObject playerObj { get; private set; }

        public static void StartRoutine(GameObject playerObj, float newSize)
        {
            var routine = playerObj.AddComponent<ObjectShrinkAnimation>();
            routine.playerObj = playerObj;
            routine.StartCoroutine(routine.run(newSize));
        }

        private IEnumerator run(float newSize)
        {
            Plugin.log("ENTERING COROUTINE OBJECT SHRINK", Plugin.LogType.Warning);
            Plugin.log("gObject: " + playerObj, Plugin.LogType.Warning);
            Transform objectTransform = playerObj.GetComponent<Transform>();
            float duration = 2f;
            float elapsedTime = 0f;
            float currentSize = playerObj.transform.localScale.x;
            if (currentSize == newSize)
                yield break;

            var modificationType = newSize < currentSize ? ShrinkRay.ModificationType.Shrinking : ShrinkRay.ModificationType.Enlarging;

            while (elapsedTime < duration && modificationType == ShrinkRay.ModificationType.Shrinking ? (currentSize > newSize) : (currentSize < newSize))
            {
                //shrinkage = -(Mathf.Pow(elapsedTime / duration, 3) - (elapsedTime / duration) * amplitude * Mathf.Sin((elapsedTime / duration) * Mathf.PI)) + 1f;
                currentSize = (float)(0.58 * Math.Sin((4 * elapsedTime / duration) + 0.81) + 0.58);
                //mls.LogFatal(shrinkage);
                objectTransform.localScale = new Vector3(currentSize, currentSize, currentSize);

                elapsedTime += Time.deltaTime;
                yield return null; // Wait for the next frame
            }

            // Ensure final scale is set to the desired value
            objectTransform.localScale = new Vector3(newSize, newSize, newSize);
            Shrinking.Instance.updatePitch();
        }
    }
}
