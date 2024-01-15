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

        public static void StartRoutine(GameObject playerObj, float newSize, Transform maskTransform, Action onComplete = null)
        {
            var routine = playerObj.AddComponent<PlayerShrinkAnimation>();
            routine.playerObj = playerObj;
            routine.StartCoroutine(routine.run(newSize, maskTransform, onComplete));
        }

        private IEnumerator run(float newSize, Transform maskTransform, Action onComplete)
        {
            Shrinking.Instance.playerTransform = playerObj.GetComponent<Transform>();
            //TODO: REPLACE WITH STORED REFERENCE
            Plugin.log(Shrinking.Instance.playerTransform.Find("ScavengerModel").Find("metarig").Find("ScavengerModelArmsOnly").ToString());
            //TODO: REPLACE WITH STORED REFERENCE
            Transform armTransform = Shrinking.Instance.playerTransform.Find("ScavengerModel").Find("metarig").Find("ScavengerModelArmsOnly");
            float duration = 2f;
            float elapsedTime = 0f;
            float currentSize = playerObj.transform.localScale.x;

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

            while (elapsedTime < duration && modificationType == ShrinkRay.ModificationType.Shrinking ? (currentSize > newSize) : (currentSize < newSize))
            {
                //shrinkage = -(Mathf.Pow(elapsedTime / duration, 3) - (elapsedTime / duration) * amplitude * Mathf.Sin((elapsedTime / duration) * Mathf.PI)) + 1f;
                currentSize = (float)(directionalForce * Math.Sin((4 * elapsedTime / duration) + 0.81) + offset);
                //mls.LogFatal(shrinkage);
                Shrinking.Instance.playerTransform.localScale = new Vector3(currentSize, currentSize, currentSize);
                maskTransform.localScale = CalcMaskScaleVec(currentSize);
                maskTransform.localPosition = CalcMaskPosVec(currentSize);
                armTransform.localScale = CalcArmScale(currentSize);

                elapsedTime += Time.deltaTime;
                yield return null; // Wait for the next frame
            }

            // Ensure final scale is set to the desired value
            Shrinking.Instance.playerTransform.localScale = new Vector3(newSize, newSize, newSize);
            maskTransform.localScale = CalcMaskScaleVec(newSize);
            maskTransform.localPosition = CalcMaskPosVec(newSize);
            armTransform.localScale = CalcArmScale(newSize);
            Shrinking.Instance.updatePitch();

            if(onComplete != null)
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
    }
}
