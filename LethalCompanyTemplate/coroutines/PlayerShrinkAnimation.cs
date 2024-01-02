using LCShrinkRay.comp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LCShrinkRay.coroutines
{
    internal class PlayerShrinkAnimation : MonoBehaviour
    {
        public GameObject playerObj { get; private set; }

        public static void StartRoutine(GameObject playerObj, float shrinkAmt, Transform maskTransform)
        {
            var routine = playerObj.AddComponent<PlayerShrinkAnimation>();
            routine.playerObj = playerObj;
            routine.StartCoroutine(routine.run(shrinkAmt, maskTransform));
        }

        private IEnumerator run(float shrinkAmt, Transform maskTransform)
        {
            Shrinking.Instance.playerTransform = playerObj.GetComponent<Transform>();
            //TODO: REPLACE WITH STORED REFERENCE
            Plugin.log(Shrinking.Instance.playerTransform.Find("ScavengerModel").Find("metarig").Find("ScavengerModelArmsOnly").ToString());
            //TODO: REPLACE WITH STORED REFERENCE
            Transform armTransform = Shrinking.Instance.playerTransform.Find("ScavengerModel").Find("metarig").Find("ScavengerModelArmsOnly");
            float duration = 2f;
            float elapsedTime = 0f;
            float shrinkage = 1f;

            while (elapsedTime < duration && shrinkage > shrinkAmt)
            {
                //shrinkage = -(Mathf.Pow(elapsedTime / duration, 3) - (elapsedTime / duration) * amplitude * Mathf.Sin((elapsedTime / duration) * Mathf.PI)) + 1f;
                shrinkage = (float)(0.58 * Math.Sin((4 * elapsedTime / duration) + 0.81) + 0.58);
                //mls.LogFatal(shrinkage);
                Shrinking.Instance.playerTransform.localScale = new Vector3(shrinkage, shrinkage, shrinkage);
                maskTransform.localScale = CalcMaskScaleVec(shrinkage);
                maskTransform.localPosition = CalcMaskPosVec(shrinkage);
                armTransform.localScale = CalcArmScale(shrinkage);

                elapsedTime += Time.deltaTime;
                yield return null; // Wait for the next frame
            }

            // Ensure final scale is set to the desired value
            Shrinking.Instance.playerTransform.localScale = new Vector3(shrinkAmt, shrinkAmt, shrinkAmt);
            maskTransform.localScale = CalcMaskScaleVec(shrinkAmt);
            maskTransform.localPosition = CalcMaskPosVec(shrinkAmt);
            armTransform.localScale = CalcArmScale(shrinkAmt);
            Shrinking.Instance.updatePitch();
        }
        private Vector3 CalcMaskPosVec(float shrinkScale)
        {
            Vector3 pos;
            float x = 0;
            float y = 0.00375f * shrinkScale + 0.05425f;
            float z = 0.005f * shrinkScale - 0.279f;
            pos = new Vector3(x, y, z);
            return pos;
        }

        private Vector3 CalcMaskScaleVec(float shrinkScale)
        {
            Vector3 pos;
            float x = 0.277f * shrinkScale + 0.2546f;
            float y = 0.2645f * shrinkScale + 0.267f;
            float z = 0.177f * shrinkScale + 0.3546f;
            pos = new Vector3(x, y, z);
            return pos;
        }

        private Vector3 CalcArmScale(float shrinkScale)
        {
            Vector3 pos;
            float x = 0.35f * shrinkScale + 0.58f;
            float y = -0.0625f * shrinkScale + 1.0625f;
            float z = -0.125f * shrinkScale + 1.15f;
            pos = new Vector3(x, y, z);
            return pos;
        }
    }
}
