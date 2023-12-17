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
using LC_API;



using static LC_API.ServerAPI.Networking;
using System.Xml.Linq;
using LC_API.ServerAPI;
using LCShrinkRay.patches;
using UnityEngine.SceneManagement;
using MonoMod.Utils;
using UnityEngine.UIElements.Internal;
using Steamworks.Ugc;
using UnityEngine.TextCore.Text;
using Steamworks.ServerList;

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
        public List<GrabbableObject> alteredGrabbedItems = new List<GrabbableObject>();
        ulong clientId = 239;
        float myScale = 1f;


        public List<string> ScreenBlockingItems = new List<string>();
        private List<GameObject> players = new List<GameObject>();

        public void Awake()
        {
            mls = BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_GUID);

            //RoundManager.Instance.allEnemyVents;

            // a list of itemnames to change
            //boombox
            //ladder
            //v-type engine
            //large Axle
            //bottles Done
            //chemical jug
            //apparatus(lung)
            //bee hive
            //cash register
            //robot
            //teapot
            //lamp
            //metal sheet     NOT ADDED
            //player(soon)

            ScreenBlockingItems.Add("Boombox(Clone)");
            ScreenBlockingItems.Add("LungApparatus(Clone)");
            ScreenBlockingItems.Add("FancyLamp(Clone)");
            ScreenBlockingItems.Add("ChemicalJug(Clone)");
            ScreenBlockingItems.Add("ExtensionLadderItem(Clone)");
            ScreenBlockingItems.Add("BinFullOfBottles(Clone)");
            ScreenBlockingItems.Add("TeaKettle(Clone)");
            ScreenBlockingItems.Add("Painting(Clone)");
            ScreenBlockingItems.Add("RobotToy(Clone)");
            ScreenBlockingItems.Add("EnginePart(Clone)");
            ScreenBlockingItems.Add("RedLocustHive(Clone)");
            ScreenBlockingItems.Add("CashRegisterItem(Clone)");
            ScreenBlockingItems.Add("Cog(Clone)");
            ScreenBlockingItems.Add("Player");
            mls.LogWarning("COUNT OF LIST IS: " + ScreenBlockingItems.Count);
            foreach(String item in ScreenBlockingItems)
            {
                mls.LogMessage('\"' + item + '\"');
            }


            
            mls.LogInfo("PENIS PENIS PENIS");

            //multiplayer networking
            Networking.GetString = (Action<string, string>)Delegate.Combine(Networking.GetString, (Action<string, string>)delegate (string data, string signature)
            {
                if (signature == "Someone...")
                {
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
                            mls.LogMessage("Looks like it must be player 0(Us)");
                            //TODO: REPLACE WITH STORED REFERENCE
                            PlayerShrinkAnimation(msgShrinkage, msgObject, GameObject.Find("ScavengerHelmet").GetComponent<Transform>());
                        }
                        //if the name isn't player, find out what player it is, extract the number, and then compare it with our client id to see if we're being shrunk
                        else if (objPlayerNum == clientId.ToString())
                        {
                            mls.LogMessage("Looks like it must be us!!!!");
                            //TODO: REPLACE WITH STORED REFERENCE
                            PlayerShrinkAnimation(msgShrinkage, msgObject, GameObject.Find("ScavengerHelmet").GetComponent<Transform>());
                        }
                        //if it's anyone or anything else, we don't care, just use ObjectShrink
                        else
                        {
                            mls.LogMessage("Looks like it must be some random person....boring...");
                            ObjectShrinkAnimation(msgShrinkage, msgObject);
                        }
                    }
                    //TODO: ADD NON-PLAYER SHRINKING
                }
            });
        }
        public float GetPlayerScale()
        {
            return myScale;
        }
        public bool IsShrunk(GameObject playerObject)
        {
            if (playerObject.transform.localScale.x < 1)
            {
                return true;
            }
            return false;
        }

        public void SetPlayerPitch(float pitch, int playerNum)
        {
            StartCoroutine(SetPlayerPitchCoroutine(pitch, playerNum));
        }

        static GameObject GetPlayerObject(int playerObjNum)
        {
            // Implement your logic to get the player object
            // For example, you could use an array or a dictionary to store player objects
            // and retrieve them based on playerObjNum
            // GameObject playerObject = ...

            string myPlayerObjectName = "Player";
            if (playerObjNum != 0)
            {
                myPlayerObjectName = "Player (" + playerObjNum.ToString() + ")";
            }
            //TODO: REPLACE WITH STORED REFERENCE
            GameObject myPlayerObject = GameObject.Find(myPlayerObjectName);
            return myPlayerObject;
        }

        private IEnumerator SetPlayerPitchCoroutine(float pitch, int playerNum)
        {
            // Get the player object based on playerObjNum
            GameObject playerObject = GetPlayerObject(playerNum);
            if (playerObject == null)
            {
                mls.LogWarning("PLAYEROBJECT IS NULL");
            }

            mls = BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_GUID);
            mls.LogInfo("SUCCESSFULLY RUNNING PITCH FROM PATCH");

            // Check if the player object is valid
            if (playerObject != null)
            {
                float duration = 6f;
                float elapsedTime = 0f;

                


                    // Get the player object's scale
                    float scale = playerObject.transform.localScale.x;
                    if(playerObject.transform == null)
                    {
                        mls.LogWarning("PLAYEROBJECT.TRANSFORM IS NULL");
                        yield break;
                    }

                    //float modifiedPitch = 1f;
                    //float modifiedPitch = -0.417f * scale + 1.417f;
                    myScale = GetPlayerObject((int)clientId).transform.localScale.x;
                    float modifiedPitch = (-0.3f * (scale - myScale) + 1f)*pitch;
                    // Set the modified pitch using the original method
                    mls.LogMessage("changing pitch of playerNum " + playerNum);
                    mls.LogMessage("\tpitch: " + modifiedPitch);
                    if(SoundManager.Instance == null)
                    {
                        mls.LogWarning("SOUNDMANAGER IS NULL");
                        yield break;
                    }
                    elapsedTime += Time.deltaTime;
                    mls.LogMessage("Elapsed time: " + elapsedTime);
                try
                {
                    SoundManager.Instance.SetPlayerPitch(modifiedPitch, playerNum);
                }
                catch (NullReferenceException e)
                {
                    mls.LogWarning("Hey...there's a null reference exception in pitch setting....not sure why!");
                    mls.LogWarning(e.ToString());
                    mls.LogMessage(e.LogDetailed);
                    mls.LogMessage(e.StackTrace.ToString());
                }
                yield return null; // Wait for the next frame
                }
        }

        private IEnumerator translateRelativeOffset(Transform referenceTransform, GrabbableObject grabbableToMove, Vector3 relativeOffset)
        {
            float delay = grabbableToMove.itemProperties.grabAnimationTime+0.2f;
            yield return new WaitForSeconds(delay);

            Debug.Log("TADAAAA IT'S TIME TO GET ANGLE!!!");

            // Get the reference rotation
            Quaternion referenceRotation = referenceTransform.rotation;

            // Calculate the relative rotation
            Quaternion relativeRotation = Quaternion.Inverse(referenceRotation) * grabbableToMove.gameObject.transform.rotation * Quaternion.Inverse(Quaternion.Euler(grabbableToMove.itemProperties.rotationOffset));

            // Apply the relative rotation to the local offset
            Vector3 offsetWorld = relativeRotation * relativeOffset;

            // Apply the offset to the current position
            Vector3 newPosition = grabbableToMove.itemProperties.positionOffset + offsetWorld;

            // Update the object's position offset
            grabbableToMove.itemProperties.positionOffset = newPosition;
        }


        public void Update()
        {
            if (!GameNetworkManagerPatch.isGameInitialized) {
                players.Clear();
            }
            else
            {
                //if our player count changes and on first run, try to update our list of players
                //mls.LogMessage("SKRAAAAAA");
                //mls.LogMessage("Connected players: " + GameNetworkManager.Instance.connectedPlayers);
                if (players.Count != GameNetworkManager.Instance.connectedPlayers)
                {
                    mls.LogWarning(players.Count);
                    mls.LogWarning(GameNetworkManager.Instance.connectedPlayers);
                    mls.LogMessage("\n a,  8a\r\n `8, `8)                            ,adPPRg,\r\n  8)  ]8                        ,ad888888888b\r\n ,8' ,8'                    ,gPPR888888888888\r\n,8' ,8'                 ,ad8\"\"   `Y888888888P\r\n8)  8)              ,ad8\"\"        (8888888\"\"\r\n8,  8,          ,ad8\"\"            d888\"\"\r\n`8, `8,     ,ad8\"\"            ,ad8\"\"\r\n `8, `\" ,ad8\"\"            ,ad8\"\"\r\n    ,gPPR8b           ,ad8\"\"\r\n   dP:::::Yb      ,ad8\"\"\r\n   8):::::(8  ,ad8\"\"\r\n   Yb:;;;:d888\"\"  Yummy\r\n    \"8ggg8P\"      Nummy");
                    mls.LogMessage("Detected miscounted players, trying to update");
                    players.Clear();
                    for (int i = 0; i < GameNetworkManager.Instance.connectedPlayers; i++)
                    {
                        try
                        {
                            mls.LogMessage("Getting player object: " + GetPlayerObject(i));
                            players.Add(GetPlayerObject(i));
                            //players.Add(GameObject.Find("Player"));
                            //mls.LogMessage(GameObject.Find("Player"));
                        }
                        catch (Exception e)
                        {
                            players.Clear();
                        }
                    }
                }
                foreach (GameObject player in players)
                {
                    //TODO: REPLACE WITH OBJECT REFERENCE
                    PlayerControllerB playerController = player.GetComponent<PlayerControllerB>();
                    if(playerController == null)
                    {
                        mls.LogWarning("playerController is fucking null goddamnit");
                    }
                    if (playerController.isHoldingObject == true )
                    {
                        mls.LogInfo("PLAYER HOLDING OBJECT");
                        GrabbableObject heldObject = playerController.currentlyHeldObjectServer;
                        //mls.LogInfo(heldObject);
                        mls.LogInfo('\"'+heldObject.name+'\"');
                        //if the held object id matches any of the ones in our array, don't do anything, else, add it to the array and change offset
                        //hasIDInList(heldObject.itemProperties.itemId, alteredGrabbedItems);
                        mls.LogInfo("Does the item match our list?");
                       
                        bool isInList = false;
                        foreach(String item in ScreenBlockingItems)
                        {
                            if (isInList == false)
                            {
                                if (item.Equals(heldObject.name))
                                {
                                    isInList = true;
                                }
                            }
                        }
                        mls.LogInfo(isInList);
                        if (!hasIDInList(heldObject.itemProperties.itemId, alteredGrabbedItems) && isInList)
                        {
                            alteredGrabbedItems.Add(heldObject);
                            //TODO: REPLACE WITH OBJECT REFERENCE
                            float scale = player.GetComponent<Transform>().localScale.x;
                            //float x = -0.25f * scale - 0.25f;
                            //float y = 0.625f * scale - 0.625f;
                            float y = 0;
                            float z = -1.04f * scale + 1.04f;
                            float x = -0.2f * scale + 0.2f;
                            //float z = 0;
                            //inverted even though my math was perfect but okay
                            Vector3 posOffsetVect = new Vector3(-x, -y, -z);
                            //heldObject.itemProperties.positionOffset = posOffsetVect;
                            //find child player eye
                            //find difference in rotation between player eye and fucking object
                            
                            StartCoroutine(translateRelativeOffset(playerController.playerEye, heldObject, posOffsetVect));
                        }
                    }
                }
                //Remove the item from the list of altered items and reset them if they're not being held
                foreach (GrabbableObject obj in alteredGrabbedItems)
                {
                    if (!obj.isHeld)
                    {
                        mls.LogMessage("removing held object!!! from the list!!!!!");
                        obj.itemProperties.positionOffset = new Vector3(0, 0, 0);
                        alteredGrabbedItems.Remove(obj);
                    }
                }




                if (clientId == 239 && GameNetworkManager.Instance.localPlayerController != null)
                {

                    clientId = GameNetworkManager.Instance.localPlayerController.playerClientId;
                    //mls.LogInfo("Instance: " + GameNetworkManager.Instance);
                    //mls.LogInfo("ClientID: " + clientId);
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
                //for each player, cycle through and find out if the player is currently holding an item
                //if yes, change the grabbleObject.item.positionOffset, and add it to a stored array of picked up items
                //if player is not holding it currently, fix it, and remove it from the array

                

                

                /*
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
                */



                if (playerTransform == null)
                {
                    try
                    {
                        //TODO: REPLACE WITH STORED REFERENCE
                        player = GameObject.Find("Player");
                        if (player != null)
                        {
                            playerTransform = player.GetComponent<Transform>();
                        }
                        //TODO: REPLACE WITH STORED REFERENCE
                        if (GameObject.Find("ScavengerHelmet") != null)
                        {
                            //TODO: REPLACE WITH STORED REFERENCE
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
                    bool iKeyPressed = false;
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
                                float newPitch = -0.417f * scale + 1.417f;
                                //(newPitch, i);
                            }
                        }
                    }
                    else if (!Keyboard.current.jKey.isPressed)
                    {
                        jKeyPressed = false;
                    }
                    if (Keyboard.current.iKey.wasPressedThisFrame && !iKeyPressed)
                    {
                        iKeyPressed = true;
                        updatePitch();
                    }
                    else if (!Keyboard.current.iKey.isPressed)
                    {
                        iKeyPressed = false;
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
                                //TODO: REPLACE WITH STORED REFERENCE
                                ObjectShrinkAnimation(scale, GameObject.Find(pPlayer));
                                //TODO: REPLACE WITH STORED REFERENCE
                                sendShrinkMessage(GameObject.Find(pPlayer), scale);
                                //SetPlayerPitch(1f, i);

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
        }
        public void updatePitch()
        {
            for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
            {
                //String pPlayer = "Player (" + i.ToString() + ")";
                if (StartOfRound.Instance.allPlayerScripts[i] != null)
                {
                    mls.LogInfo("Altering player voice pitches");
                    SetPlayerPitch(1f, i);
                }
            }
        }

            private bool hasIDInList(int itemId, List<GrabbableObject> alteredGrabbedItems)
            {
                foreach (GrabbableObject item in alteredGrabbedItems)
                {
                    if (item.itemProperties.itemId == itemId)
                    {
                        return true;
                    }
                }
                return false;
            }

            public void sendShrinkMessage(GameObject shrinkObject, float shrinkage)
            {
                //This turns the object into a searchable string
                String rawStr = shrinkObject.ToString();
                String newStr = shrinkObject.ToString().Substring(0, rawStr.LastIndexOf('(') - 1);
                mls.LogMessage("Sending message that an object is shrinking! Object: \"" + newStr + "\" Shrinkage: " + shrinkage);
                LC_API.ServerAPI.Networking.Broadcast(newStr + ',' + shrinkage.ToString(), "Someone...");
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
                objectTransform.localScale = new Vector3(shrinkAmt, shrinkAmt, shrinkAmt);
                updatePitch();
        }





            //Player Shrink animation, shrinks a player over a sinusoidal curve for a duration. Requires the player and mask transforms.
            public void PlayerShrinkAnimation(float shrinkAmt, GameObject player, Transform maskTransform)
            {
                StartCoroutine(PlayerShrinkAnimationCoroutine(shrinkAmt, player, maskTransform));
            }

            private IEnumerator PlayerShrinkAnimationCoroutine(float shrinkAmt, GameObject player, Transform maskTransform)
            {
                playerTransform = player.GetComponent<Transform>();
                //TODO: REPLACE WITH STORED REFERENCE
                mls.LogInfo(playerTransform.Find("ScavengerModel").Find("metarig").Find("ScavengerModelArmsOnly"));
                //TODO: REPLACE WITH STORED REFERENCE
                Transform armTransform = playerTransform.Find("ScavengerModel").Find("metarig").Find("ScavengerModelArmsOnly");
                float amplitude = 0.5f;
                float duration = 2f;
                float elapsedTime = 0f;
                float shrinkage = 1f;

                while (elapsedTime < duration && shrinkage > shrinkAmt)
                {
                    //shrinkage = -(Mathf.Pow(elapsedTime / duration, 3) - (elapsedTime / duration) * amplitude * Mathf.Sin((elapsedTime / duration) * Mathf.PI)) + 1f;
                    shrinkage = (float)(0.58 * Math.Sin((4 * elapsedTime / duration) + 0.81) + 0.58);
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
                updatePitch();
        }
            public Vector3 CalcMaskPosVec(float shrinkScale)
            {
                Vector3 pos;
                float x = 0;
                float y = 0.00375f * shrinkScale + 0.05425f;
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
