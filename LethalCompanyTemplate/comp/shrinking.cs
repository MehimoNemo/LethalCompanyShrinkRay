using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using GameNetcodeStuff;



namespace LCShrinkRay.comp
{
    internal class shrinking : MonoBehaviour
    {
        Transform playerTransform;
        Transform helmetHudTransform;
        internal ManualLogSource mls;
        public void Awake()
        {
            mls = BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_GUID);
            mls.LogInfo("PENIS PENIS PENIS");
            playerTransform = GameObject.Find("Player").GetComponent<Transform>();
            helmetHudTransform = GameObject.Find("ScavengerHelmet").GetComponent<Transform>();
            helmetHudTransform.localPosition = new Vector3(-0.0f, 0.058f, -0.274f);
        }
        public void Update()
        {
            //mls.LogInfo("\n\n\n\n\n\n HELP \n\n\n\n\n\n");
            if (Keyboard.current.nKey.wasPressedThisFrame)
            {
                mls.LogInfo("Shrinking player models");
                float scale = 0.2f;
                PlayerShrinkAnimation(scale, playerTransform, helmetHudTransform);
            }

        }
        public void ObjectShrinkAnimation(float shrinkAmt, Transform objectTransform)
        {

            float shrinkage;
            float amplitude = 0.5f;
            DateTime oldTime = DateTime.Now;
            while (true)
            {
                TimeSpan timeSpan = DateTime.Now - oldTime;
                float time = (float)timeSpan.TotalSeconds;
                shrinkage = (float)-((Math.Pow(time, 3) - time * amplitude * Math.Sin(time * Math.PI)));
                if (shrinkage < shrinkAmt)
                {
                    objectTransform.localScale = new Vector3(shrinkAmt, shrinkAmt, shrinkAmt);
                    break;
                }

            }
        }
        public void PlayerShrinkAnimation(float shrinkAmt, Transform playerTransform, Transform maskTransform)
        {

            float shrinkage;
            float amplitude = 0.5f;
            float duration = 3f;
            DateTime oldTime = DateTime.Now;
            while (true)
            {
                TimeSpan timeSpan = DateTime.Now - oldTime;
                float time = (float)timeSpan.TotalSeconds/duration;
                shrinkage = (float)-((Math.Pow(time, 3) - time * amplitude * Math.Sin(time * Math.PI)))+1;
                if (shrinkage < shrinkAmt)
                {
                    playerTransform.localScale = new Vector3(shrinkAmt, shrinkAmt, shrinkAmt);
                    maskTransform.localScale = CalcMaskScaleVec(shrinkAmt);
                    maskTransform.localPosition= CalcMaskPosVec(shrinkAmt);
                    break;
                }
                playerTransform.localScale = new Vector3(shrinkage, shrinkage, shrinkage);
                maskTransform.localScale = CalcMaskScaleVec(shrinkage);
                maskTransform.localPosition = CalcMaskPosVec(shrinkage);

            }
        }
        public Vector3 CalcMaskPosVec(float shrinkScale)
        {
            Vector3 pos;
            float x = 0;
            float y = 0.00375f*shrinkScale+0.05425f;
            float z = 0.005f * shrinkScale - 0.279f;
            pos = new Vector3(x, y, z);
            return pos;
        }

        public Vector3 CalcMaskScaleVec(float shrinkScale)
        {
            Vector3 pos;
            float x = 0.277f * shrinkScale + 0.2546f;
            float y = 0.2645f * shrinkScale + 0.267f;
            float z = 0.177f * shrinkScale + 0.3546f;
            pos = new Vector3(x, y, z);
            return pos;
        }
    }
}
