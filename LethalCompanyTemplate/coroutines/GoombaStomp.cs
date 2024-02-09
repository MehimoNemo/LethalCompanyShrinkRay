using LCShrinkRay.comp;
using System;
using System.Collections;
using UnityEngine;

namespace LCShrinkRay.coroutines
{
    internal class GoombaStomp : MonoBehaviour
    {
        public GameObject playerObj { get; private set; }

        public static void StartRoutine(GameObject playerObj, Action onComplete = null)
        {
            var routine = playerObj.AddComponent<GoombaStomp>();
            routine.playerObj = playerObj;
            routine.StartCoroutine(routine.Run(onComplete));
        }

        private IEnumerator Run(Action onComplete = null)
        {
            AnimationCurve scaleCurve = new AnimationCurve(
                new Keyframe(0, 0.4f),
                new Keyframe(0.05f, 0.05f),
                new Keyframe(0.85f, 0.1f),
                new Keyframe(1f, 0.4f)
            );
            scaleCurve.preWrapMode = WrapMode.PingPong;
            scaleCurve.postWrapMode = WrapMode.PingPong;

            AnimationCurve stretchCurve = new AnimationCurve(
                new Keyframe(0, 0.7f),
                new Keyframe(0.5f, 0.6f),
                new Keyframe(1f, 0.4f)
            );

            float duration = 5f;
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                float scaleValue = scaleCurve.Evaluate(elapsedTime / duration);
                float stretchValue = stretchCurve.Evaluate(elapsedTime / duration);

                playerObj.transform.localScale = new Vector3(stretchValue, scaleValue, stretchValue);

                //Plugin.log(playerObj.transform.localScale.ToString());
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            if(onComplete != null)
                onComplete();
        }
    }
}
