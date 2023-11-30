﻿using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using GameNetcodeStuff;
using System.Collections;
using Unity.Netcode;
using System.Linq;
using LC_API;



using static LC_API.ServerAPI.Networking;
using System.Xml.Linq;
using LC_API.ServerAPI;

namespace LCShrinkRay.comp
{
    internal class Shrinking : MonoBehaviour
    {
        GameObject myPlayerObject;
        GameObject player;
        Transform playerTransform;
        public GameObject player1Object;
        Transform player1Transform;
        Transform helmetHudTransform;
        private static ManualLogSource mls;
        public static List<GameObject> grabbables = new List<GameObject>();
        ulong clientId = 239;
        float myScale = 1f;



        public void Awake()
        {
            
                mls = BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_GUID);
                mls.LogInfo("PENIS PENIS PENIS");
                /*playerTransform = GameObject.Find("Player").GetComponent<Transform>();
                helmetHudTransform = GameObject.Find("ScavengerHelmet").GetComponent<Transform>();
                helmetHudTransform.localPosition = new Vector3(-0.0f, 0.058f, -0.274f);*/


                Networking.GetString = (GotStringEventDelegate)delegate (string data, string signature)
                {
                    if (signature == "Someone...")
                    {
                        mls.LogWarning("THREAD NAME");
                        mls.LogWarning(System.Threading.Thread.CurrentThread.Name);
                        mls.LogMessage("Recieveing message that an object is shrinking! Object: \"" + data + "\"");
                        mls.LogInfo("DATA");
                        mls.LogInfo(data);
                        mls.LogInfo(signature);

                        String[] splitStr = data.Split(',');
                        GameObject msgObject;
                        try
                        {
                            msgObject = GameObject.Find(splitStr[0]);
                            mls.LogWarning("Found the gosh dang game object: \"" + msgObject + "\"!");
                        }
                        catch (Exception e)
                        {
                            mls.LogWarning("Could not find the gosh dang game object named \"" + splitStr[0] + "\"");
                            msgObject = null;
                        }
                        float msgShrinkage = float.Parse(splitStr[1]);
                        mls.LogMessage(splitStr[1]);

                        mls.LogMessage("IS THIS WHERE IT'S BREAKING????");

                        String objPlayerNum = "0";
                        if (splitStr[0].Contains('('))
                        {
                            objPlayerNum = splitStr[0].Substring(splitStr[0].IndexOf("(") + 1, splitStr[0].IndexOf(")") - splitStr[0].IndexOf("(") - 1);
                        }
                        mls.LogMessage("objPlayerNum: " + objPlayerNum);

                        //if object getting shrunk is us, let's shrink using playerShrinkAnimation
                        //else, just use object
                        //actually on second thought, let's just always use playerShrinkAnimation, everything's wrapped in try's anyways
                        mls.LogMessage("OKAY HERE IS THE OBJECT TAG BELOW THIS LINE!!!!");
                        mls.LogMessage("Object tag is " + msgObject.tag);
                        mls.LogMessage("The client Id is: " + clientId.ToString());
                        mls.LogMessage("The object name is: " + msgObject.name);
                        if (msgObject.tag == "Player")
                        {
                            mls.LogMessage("Looks like it must be a player");
                            //if the name is just player with not parenthesis, and we're player 0, use playerShrinkAnimation
                            if (!(msgObject.name.Contains("(")) && clientId == 0)
                            {
                                mls.LogMessage("Looks like it must be player 0");
                                PlayerShrinkAnimation(msgShrinkage, msgObject, GameObject.Find("ScavengerHelmet").GetComponent<Transform>());
                                if (msgShrinkage < 1)
                                {
                                    // Add the GrabbableObject script to the existing object
                                    GrabbableObject grabbableObject = msgObject.GetComponentByName("NetworkObject").gameObject.AddComponent<GrabbableObject>();
                                    grabbableObject.grabbable = true;
                                }
                            }
                            //if the name isn't player, find out what player it is, extract the number, and then compare it with our client id to see if we're being shrunk
                            else if (objPlayerNum == clientId.ToString())
                            {
                                mls.LogMessage("Looks like it must be us!!!!");
                                PlayerShrinkAnimation(msgShrinkage, msgObject, GameObject.Find("ScavengerHelmet").GetComponent<Transform>());
                                if (msgShrinkage < 1)
                                {
                                    // Add the GrabbableObject script to the existing object
                                    GrabbableObject grabbableObject = msgObject.GetComponentByName("NetworkObject").gameObject.AddComponent<GrabbableObject>();
                                    grabbableObject.grabbable = true;
                                }
                            }
                            //if it's anyone or anything else, we don't care, just use ObjectShrink
                            else
                            {
                                mls.LogMessage("Looks like it must be some random person....boring...");
                                ObjectShrinkAnimation(msgShrinkage, msgObject);
                                if (msgShrinkage < 1)
                                {
                                    // Add the GrabbableObject script to the existing object
                                    GrabbableObject grabbableObject = msgObject.GetComponentByName("NetworkObject").gameObject.AddComponent<GrabbableObject>();
                                    grabbableObject.grabbable = true;
                                }
                            }
                        }
                        //TODO: ADD NON-PLAYER SHRINKING
                    }
                };
            }
        public float getPlayerScale()
        {
            return myScale;
        }
        public void Update()
        {
            
                //SoundManager.Instance.SetPlayerPitch(1+(-0.417f * myScale + 0.417f), 0);
                mls.LogMessage(SoundManager.Instance.playerVoicePitchTargets[0]);




                if (clientId == 239 && GameNetworkManager.Instance.localPlayerController != null)
                {

                    clientId = GameNetworkManager.Instance.localPlayerController.playerClientId;
                    //mls.LogInfo("Instance: " + GameNetworkManager.Instance);
                    mls.LogInfo("ClientID: " + clientId);
                }
                /*else if (clientId != 239)
                {
                    string myPlayerObjectName = "Player";
                    if (clientId != 0) { 
                        myPlayerObjectName = "Player ("+clientId.ToString()+")";
                    }
                    myPlayerObject = GameObject.Find(myPlayerObjectName);
                    myScale = myPlayerObject.transform.localScale.x;
                    player.GetComponent<PlayerControllerB>() = -0.417f * myScale + 0.417f;
                    mls.LogMessage(player.GetComponent<PlayerControllerB>().drunkness);
                }*/
                //If player picks up something and is short, change the grabbleObject.item.positionOffset by -0.2 0.5 -0.5(still need to test with other objects besides apparatus)
                //for each grabbable object, if public PlayerControllerB playerHeldBy does not equal null, find out which player and if they're shrunk. If they are, change the item offset.
                grabbables.Clear();

                GrabbableObject[] array = UnityEngine.Object.FindObjectsOfType<GrabbableObject>();

                //mls.LogMessage(grabbables);
                for (int i = 0; i < array.Length; i++)
                {
                    PlayerControllerB holdingPlayer = array[i].playerHeldBy;
                    //Vector3 objectOffset = holdingPlayer.currentlyHeldObject.itemProperties.positionOffset;

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
                            array[i].itemProperties.positionOffset = new Vector3(0, 0, 0);
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
                        if (player != null)
                        {
                            playerTransform = player.GetComponent<Transform>();
                        }
                        if (GameObject.Find("ScavengerHelmet") != null)
                        {
                            helmetHudTransform = GameObject.Find("ScavengerHelmet").GetComponent<Transform>();
                            helmetHudTransform.localPosition = new Vector3(-0.0f, 0.058f, -0.274f);
                            mls.LogInfo("Player transform got!");
                        }
                    }
                    catch (Exception e) { }
                    try
                    {
                        player1Object = GameObject.Find("Player (1)");
                        if (player1Object != null)
                        {
                            player1Transform = player1Object.GetComponent<Transform>();
                        }
                    }
                    catch (Exception e) { }
                }
                //mls.LogInfo("\n\n\n\n\n\n HELP \n\n\n\n\n\n");
                try
                {
                    bool nKeyPressed = false;
                    bool mKeyPressed = false;
                    bool jKeyPressed = false;
                    bool kKeyPressed = false;
                    bool oKeyPressed = false;
                    if (Keyboard.current.oKey.wasPressedThisFrame && !oKeyPressed)
                    {
                        oKeyPressed = true;
                        mls.LogInfo("Simulating fake broadcast");
                        //ShGetString("Player,"+(0.4f).ToString(), "Someone...");
                        //this is an old debug tool, no longer works, i should probably figure out how to recreate it...
                        Networking.GetString?.Invoke("Player," + (0.4f).ToString(), "Someone...");
                    }
                    else if (!Keyboard.current.oKey.isPressed)
                    {
                        oKeyPressed = false;
                    }
                    if (Keyboard.current.nKey.wasPressedThisFrame && !nKeyPressed)
                    {
                        nKeyPressed = true;
                        mls.LogInfo("Shrinking player model");
                        float scale = 0.4f;
                        PlayerShrinkAnimation(scale, player, helmetHudTransform);
                        sendShrinkMessage(player, scale);
                    }
                    else if (!Keyboard.current.nKey.isPressed)
                    {
                        nKeyPressed = false;
                    }
                    if (Keyboard.current.mKey.wasPressedThisFrame && !mKeyPressed)
                    {
                        mKeyPressed = true;
                        mls.LogInfo("Growing player model");
                        float scale = 1f;
                        PlayerShrinkAnimation(scale, player, helmetHudTransform);
                        sendShrinkMessage(player, scale);
                    }
                    else if (!Keyboard.current.mKey.isPressed)
                    {
                        mKeyPressed = false;
                    }
                    if (Keyboard.current.jKey.wasPressedThisFrame && !jKeyPressed)
                    {
                        jKeyPressed = true;

                        float scale = 0.4f;
                        int i;
                        for (i = 1; i < GameNetworkManager.Instance.connectedPlayers; i++)
                        {
                            String pPlayer = "Player (" + i.ToString() + ")";
                            if (GameObject.Find(pPlayer) != null)
                            {
                                mls.LogInfo("Shrinking player(1) model");
                                ObjectShrinkAnimation(scale, GameObject.Find(pPlayer));
                                sendShrinkMessage(GameObject.Find(pPlayer), scale);
                            }
                        }
                    }
                    else if (!Keyboard.current.jKey.isPressed)
                    {
                        jKeyPressed = false;
                    }
                    if (Keyboard.current.kKey.wasPressedThisFrame && !kKeyPressed)
                    {
                        kKeyPressed = true;
                        mls.LogInfo("Growing player(1) model");
                        float scale = 1f;
                        int i;
                        for (i = 1; i < GameNetworkManager.Instance.connectedPlayers; i++)
                        {
                            String pPlayer = "Player (" + i.ToString() + ")";
                            if (GameObject.Find(pPlayer) != null)
                            {
                                mls.LogInfo("Shrinking player(1) model");
                                ObjectShrinkAnimation(scale, GameObject.Find(pPlayer));
                                sendShrinkMessage(GameObject.Find(pPlayer), scale);
                            }
                        }
                    }
                    else if (!Keyboard.current.kKey.isPressed)
                    {
                        kKeyPressed = false;
                    }
                }
                catch (Exception e) { }
            }
        public void lateUpdate()
        {
                SoundManager.Instance.playerVoicePitchTargets[0] = 1.5f;
            
        }
        public void sendShrinkMessage(GameObject shrinkObject, float shrinkage)
        {
            //This turns the object into a searchable string
            String rawStr = shrinkObject.ToString();
            String newStr = shrinkObject.ToString().Substring(0, rawStr.LastIndexOf('(') - 1);
            mls.LogMessage("Sending message that an object is shrinking! Object: \""+newStr+"\" Shrinkage: "+shrinkage);
            LC_API.ServerAPI.Networking.Broadcast(newStr+','+shrinkage.ToString(), "Someone...");
        }

        //object shrink animation infrastructure!
        public void ObjectShrinkAnimation(float shrinkAmt, GameObject gObject)
        {
            if (gObject != null)
            {
                mls.LogWarning("LOOKS GOOD SENDING IT TO THE COROUTINE!!!!!");
                StartCoroutine(ObjectShrinkAnimationCoroutine(shrinkAmt, gObject));
            }
            else
            {
                mls.LogMessage("gObject is null...");
            }
        }

        private IEnumerator ObjectShrinkAnimationCoroutine(float shrinkAmt, GameObject gObject)
        {
            mls.LogWarning("ENTERING COROUTINE OBJECT SHRINK");
            mls.LogWarning("gObject: " + gObject);
            Transform objectTransform = gObject.GetComponent<Transform>();
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
