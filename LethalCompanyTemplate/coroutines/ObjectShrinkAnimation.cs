using LCShrinkRay.comp;
using System;
using System.Collections;
using UnityEngine;

namespace LCShrinkRay.coroutines
{
    internal class ObjectShrinkAnimation : MonoBehaviour
    {
        public GameObject playerObj { get; private set; }

        public static void StartRoutine(GameObject playerObj, float shrinkAmt)
        {
            var routine = playerObj.AddComponent<ObjectShrinkAnimation>();
            routine.playerObj = playerObj;
            routine.StartCoroutine(routine.run(shrinkAmt));
        }

        private IEnumerator run(float shrinkAmt)
        {
            Plugin.log("ENTERING COROUTINE OBJECT SHRINK", Plugin.LogType.Warning);
            Plugin.log("gObject: " + playerObj, Plugin.LogType.Warning);
            Transform objectTransform = playerObj.GetComponent<Transform>();
            float duration = 2f;
            float elapsedTime = 0f;
            float shrinkage = 1f;

            while (elapsedTime < duration && shrinkage > shrinkAmt)
            {
                //shrinkage = -(Mathf.Pow(elapsedTime / duration, 3) - (elapsedTime / duration) * amplitude * Mathf.Sin((elapsedTime / duration) * Mathf.PI)) + 1f;
                shrinkage = (float)(0.58 * Math.Sin((4 * elapsedTime / duration) + 0.81) + 0.58);
                //mls.LogFatal(shrinkage);
                objectTransform.localScale = new Vector3(shrinkage, shrinkage, shrinkage);

                elapsedTime += Time.deltaTime;
                yield return null; // Wait for the next frame
            }

            // Ensure final scale is set to the desired value
            objectTransform.localScale = new Vector3(shrinkAmt, shrinkAmt, shrinkAmt);
            Shrinking.Instance.updatePitch();
        }
    }
}
