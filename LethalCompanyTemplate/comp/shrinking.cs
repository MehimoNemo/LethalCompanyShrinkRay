using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using GameNetcodeStuff;
using System.Collections;
using Unity.Netcode;
using System.Linq;

namespace LCShrinkRay.comp
{
    internal class shrinking : MonoBehaviour
    {
        GameObject player;
        Transform playerTransform;
        Transform player1Transform;
        Transform helmetHudTransform;
        internal ManualLogSource mls;
        public static List<GameObject> grabbables = new List<GameObject>();
        public void Awake()
        {
            mls = BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_GUID);
            mls.LogInfo("PENIS PENIS PENIS");
            /*playerTransform = GameObject.Find("Player").GetComponent<Transform>();
            helmetHudTransform = GameObject.Find("ScavengerHelmet").GetComponent<Transform>();
            helmetHudTransform.localPosition = new Vector3(-0.0f, 0.058f, -0.274f);*/ 
        }
        public void Update()
        {
            //If player picks up something and is short, change the grabbleObject.item.positionOffset by -0.2 0.5 -0.5(still need to test with other objects besides apparatus)
            //for each grabbable object, if public PlayerControllerB playerHeldBy does not equal null, find out which player and if they're shrunk. If they are, change the item offset.
            grabbables.Clear();
            GrabbableObject[] array = UnityEngine.Object.FindObjectsOfType<GrabbableObject>();
            
            //mls.LogMessage(grabbables);

            for (int i = 0; i < array.Length; i++)
            {
                PlayerControllerB holdingPlayer = array[i].playerHeldBy;
                
                if (holdingPlayer != null)
                {
                    Transform holdingPlayerTransform = holdingPlayer.GetComponent<Transform>();
                    mls.LogInfo("Found player holding object!");
                    mls.LogInfo(holdingPlayer);
                    mls.LogInfo(array[i].itemProperties.positionOffset);
                    //if player scale is less than 1 and we've not finished scaling the position
                    if (array[i].itemProperties.positionOffset != new Vector3(-0.2f, 0.5f, -0.5f) && holdingPlayerTransform.localScale.x != 1f)
                    {
                        //then scale the offset position appropriately
                        float scale = holdingPlayerTransform.localScale.x;
                        float x = -0.25f * scale - 0.25f;
                        float y = 0.625f * scale - 0.625f;
                        float z = -0.625f * scale + 0.625f;
                        //inverted even though my math was perfect but okay
                        Vector3 posOffsetVect = new Vector3(-x, -y, -z);
                        array[i].itemProperties.positionOffset = posOffsetVect;

                    }
                    // else if player scale is normal or bigger and we've not finished resetting the position
                    else if (array[i].itemProperties.positionOffset != new Vector3(0, 0, 0) && holdingPlayerTransform.localScale.x == 1f)
                    {
                        //then reset it
                        array[i].itemProperties.positionOffset = new Vector3 (0, 0, 0);
                    }
                }
                //This piece of code is to reset objects after they get set down
                else
                {
                    array[i].itemProperties.positionOffset = new Vector3(0, 0, 0);
                }


            }




            if (playerTransform == null)
            {
                try
                {
                    player = GameObject.Find("Player");
                    playerTransform = player.GetComponent<Transform>();
                    helmetHudTransform = GameObject.Find("ScavengerHelmet").GetComponent<Transform>();
                    helmetHudTransform.localPosition = new Vector3(-0.0f, 0.058f, -0.274f);
                    mls.LogInfo("Player transform got!");
                } catch(Exception e) { }
                try
                {
                    player1Transform = GameObject.Find("Player (1)").GetComponent<Transform>();
                } catch(Exception e) { }
            }
            //mls.LogInfo("\n\n\n\n\n\n HELP \n\n\n\n\n\n");
            try
            {
                if (Keyboard.current.nKey.wasPressedThisFrame)
                {
                    mls.LogInfo("Shrinking player model");
                    float scale = 0.4f;
                    PlayerShrinkAnimation(scale, player, helmetHudTransform);
                }
                if (Keyboard.current.mKey.wasPressedThisFrame)
                {
                    mls.LogInfo("Growing player model");
                    float scale = 1f;
                    PlayerShrinkAnimation(scale, player, helmetHudTransform);
                }
                if (Keyboard.current.jKey.wasPressedThisFrame)
                {
                    mls.LogInfo("Shrinking player(1) model");
                    float scale = 0.4f;
                    ObjectShrinkAnimation(scale, player1Transform);
                }
                if (Keyboard.current.kKey.wasPressedThisFrame)
                {
                    mls.LogInfo("Growing player(1) model");
                    float scale = 1f;
                    ObjectShrinkAnimation(scale, player1Transform);
                }
            }
            catch(Exception e) { }
        }
        


        //object shrink animation infrastructure!
        public void ObjectShrinkAnimation(float shrinkAmt, Transform objectTransform)
        {
            StartCoroutine(ObjectShrinkAnimationCoroutine(shrinkAmt, objectTransform));
        }

        private IEnumerator ObjectShrinkAnimationCoroutine(float shrinkAmt, Transform objectTransform)
        {
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
            objectTransform.localScale = new Vector3(shrinkAmt, shrinkAmt, shrinkAmt);;
        }





        //Player Shrink animation, shrinks a player over a sinusoidal curve for a duration. Requires the player and mask transforms.
        public void PlayerShrinkAnimation(float shrinkAmt, GameObject player, Transform maskTransform)
        {
            StartCoroutine(PlayerShrinkAnimationCoroutine(shrinkAmt, player, maskTransform));
        }

        private IEnumerator PlayerShrinkAnimationCoroutine(float shrinkAmt, GameObject player, Transform maskTransform)
        {
            playerTransform = player.GetComponent<Transform>();
            mls.LogInfo(playerTransform.Find("ScavengerModel").Find("metarig").Find("ScavengerModelArmsOnly"));
            Transform armTransform = playerTransform.Find("ScavengerModel").Find("metarig").Find("ScavengerModelArmsOnly");
            float amplitude = 0.5f;
            float duration = 2f;
            float elapsedTime = 0f;
            float shrinkage = 1f;

            while (elapsedTime < duration && shrinkage>shrinkAmt)
            {
                //shrinkage = -(Mathf.Pow(elapsedTime / duration, 3) - (elapsedTime / duration) * amplitude * Mathf.Sin((elapsedTime / duration) * Mathf.PI)) + 1f;
                shrinkage = (float)(0.58 * Math.Sin((4 * elapsedTime / duration)+0.81) + 0.58);
                //mls.LogFatal(shrinkage);
                playerTransform.localScale = new Vector3(shrinkage, shrinkage, shrinkage);
                maskTransform.localScale = CalcMaskScaleVec(shrinkage);
                maskTransform.localPosition = CalcMaskPosVec(shrinkage);
                armTransform.localScale = CalcArmScale(shrinkage);

                elapsedTime += Time.deltaTime;
                yield return null; // Wait for the next frame
            }

            // Ensure final scale is set to the desired value
            playerTransform.localScale = new Vector3(shrinkAmt, shrinkAmt, shrinkAmt);
            maskTransform.localScale = CalcMaskScaleVec(shrinkAmt);
            maskTransform.localPosition = CalcMaskPosVec(shrinkAmt);
            armTransform.localScale = CalcArmScale(shrinkAmt);
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

        public Vector3 CalcArmScale(float shrinkScale)
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
