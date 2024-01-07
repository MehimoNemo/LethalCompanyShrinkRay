using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using GameNetcodeStuff;

using LCShrinkRay.patches;
using System.IO;
using System.Reflection;
using LethalLib.Modules;
using LCShrinkRay.Config;
using LC_API.Networking;
using Unity.Netcode;
using LC_API.ServerAPI;
using System.Linq;
using Newtonsoft.Json;

namespace LCShrinkRay.comp
{
    internal class Shrinking
    {
        private static Shrinking instance = null;
        private static readonly object padlock = new object();

        public static Shrinking Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                        instance = new Shrinking();

                    return instance;
                }
            }
        }


        GameObject player;
        public GameObject player1Object;
        public Transform helmetHudTransform;
        public static List<GameObject> grabbables = new List<GameObject>();
        public static List<GrabbableObject> alteredGrabbedItems = new List<GrabbableObject>();

        public float myScale = 1f;

        public Transform playerTransform, player1Transform;
        public ulong clientId = 239;


        public List<string> ScreenBlockingItems = new List<string>();
        private List<GameObject> players = new List<GameObject>();

        public void setup()
        {
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
            Plugin.log("COUNT OF LIST IS: " + ScreenBlockingItems.Count, Plugin.LogType.Warning);
            foreach (string item in ScreenBlockingItems)
            {
                Plugin.log('\"' + item + '\"');
            }

            AddShrinkRayToGame();
        }


        // Multiplayer Networking
        internal class ShrinkData
        {
            public string playerObjName { get; set; }
            public float shrinkage { get; set; }
        }

        [NetworkMessage("OnShrinking")]
        public static void OnShrinking(ulong sender, ShrinkData data)
        {
            GameObject msgObject;
            try
            {
                msgObject = GameObject.Find(data.playerObjName);
                Plugin.log("Found the gosh dang game object: \"" + msgObject + "\"!", Plugin.LogType.Warning);
            }
            catch (Exception e)
            {
                Plugin.log("Could not find the gosh dang game object named \"" + data.playerObjName + "\". Reason: " + e.Message, Plugin.LogType.Warning);
                msgObject = null;
            }
            Plugin.log("Shrinkage: " + data.shrinkage);

            ulong playerID = 0ul;
            if (data.playerObjName.Contains('('))
            {
                int startIndex = data.playerObjName.IndexOf("(");
                int endIndex = data.playerObjName.IndexOf(")");
                playerID = ulong.Parse(data.playerObjName.Substring(startIndex + 1, endIndex - startIndex - 1));
            }
            Plugin.log("objPlayerNum: " + playerID.ToString());

            //if object getting shrunk is us, let's shrink using playerShrinkAnimation
            //else, just use object
            Instance.ShrinkPlayer(msgObject, data.shrinkage, playerID);
        }

        private static bool isGoombaCoroutineRunning = false;

        [NetworkMessage("OnGoomba")]
        public static void OnGoomba(ulong sender, string playerID)
        {
            Plugin.log("A goomba...... stompin' on player " + playerID);

            // Check if the goomba coroutine is already running
            if (!isGoombaCoroutineRunning)
            {
                coroutines.GoombaStomp.StartRoutine(GetPlayerObject(ulong.Parse(playerID)));
                isGoombaCoroutineRunning = true;
            }
        }

        [NetworkMessage("OnListTransmit")]
        public static void OnListTransmit(ulong sender, string data)
        {
            Plugin.log("Oh hey!! There's that list I needed!!!!", Plugin.LogType.Error);
            Plugin.log("\t" +data);
            List<(ulong, ulong)> tuple = StringToTupleList(data);
            Plugin.log("\t{" + tuple[0].Item1+", " + tuple[0].Item2 + "}, {" + tuple[1].Item1 + ", " + tuple[1].Item2 + "}");
            Shrinking.Instance.UpdateGrabbablePlayerObjectsClient(tuple);
        }

        [NetworkMessage("OnListRequest")]
        public static void OnListRequest(ulong sender)
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                Plugin.log("List Requested!", Plugin.LogType.Error);
                Shrinking.Instance.BroadcastGrabbedPlayerObjectsList();
            }
        }

        static string TupleListToString(List<(ulong, ulong)> tupleList)
        {
            return JsonConvert.SerializeObject(tupleList);
        }

        static List<(ulong, ulong)> StringToTupleList(string jsonString)
        {
            return JsonConvert.DeserializeObject<List<(ulong, ulong)>>(jsonString);
        }

        public void OnGoombaCoroutineComplete()
        {
            isGoombaCoroutineRunning = false;
        }

        private static void CheckIfPlayerAbove()
        {
            // Cast a ray upwards to check for the player above
            RaycastHit hit;
            if (Physics.Raycast(StartOfRound.Instance.localPlayerController.gameplayCamera.transform.position, StartOfRound.Instance.localPlayerController.gameObject.transform.up, out hit, 1f, StartOfRound.Instance.playersMask, QueryTriggerInteraction.Ignore))
            {
                Transform hitObject = hit.collider.gameObject.GetComponent<PlayerControllerB>().transform;
                if (1f == hitObject.localScale.x)
                {
                    if (ModConfig.Instance.values.jumpOnShrunkenPlayers)
                    {
                        Plugin.log("WE GETTING GOOMBAD");
                        Network.Broadcast("OnGoomba", StartOfRound.Instance.localPlayerController.playerClientId.ToString());
                        coroutines.GoombaStomp.StartRoutine(StartOfRound.Instance.localPlayerController.gameObject);
                    }
                }
            }
        }
        public static GameObject grabbablePlayerPrefab;
        public static void AddShrinkRayToGame() // todo: Move to shrinkRay.cs
        {
            Plugin.log("Addin shrink rayyy");
            //Plugin.log("TRYING TO ADD ASSET TO THING: `1");
            string assetDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "shrinkasset");
            //Plugin.log("TRYING TO ADD ASSET TO THING: `2");
            AssetBundle UpgradeAssets = AssetBundle.LoadFromFile(assetDir);

            //Lethal Company_Data
            Plugin.log("TRYING TO ADD ASSET TO THING: `3");
            Item shrinkRayItem = UpgradeAssets.LoadAsset<Item>("ShrinkRayItem.asset");
            //I SWEAR TO GO IF THE PROBLEM WAS A LOWERCASE G I WILL KILL ALL OF MANKIND
            Item grabbablePlayerItem = UpgradeAssets.LoadAsset<Item>("grabbablePlayerItem.asset");
            if(grabbablePlayerItem == null)
            {
                Plugin.log("\n\nFUCK WHY IS IT NULL???\n\n");
            }
            Plugin.log("TRYING TO ADD ASSET TO THING: `4");
            //shrinkRayItem.creditsWorth = ModConfig.Instance.values.shrinkRayCost;
            shrinkRayItem.creditsWorth = 0;
            Plugin.log("TRYING TO ADD ASSET TO THING: `5");
            shrinkRayItem.spawnPrefab.transform.localScale = new Vector3(1f, 1f, 1f);
            Plugin.log("TRYING TO ADD ASSET TO THING: `6");
            ShrinkRay visScript = shrinkRayItem.spawnPrefab.AddComponent<ShrinkRay>();
            GrabbablePlayerObject grabbyScript = grabbablePlayerItem.spawnPrefab.AddComponent<GrabbablePlayerObject>();
            PhysicsProp grabbyPhysProp = shrinkRayItem.spawnPrefab.GetComponent<PhysicsProp>();
            grabbyScript.itemProperties = grabbyPhysProp.itemProperties;
            

            Plugin.log("TRYING TO ADD ASSET TO THING: `7");
            visScript.itemProperties = shrinkRayItem;
            grabbyScript.itemProperties = grabbablePlayerItem;
            if (grabbyScript.itemProperties == null)
            {
                Plugin.log("\n\nSHIT HOW IS IT NULL???\n\n");
            }
            Plugin.log("TRYING TO ADD ASSET TO THING: `8");
            PhysicsProp.Destroy(grabbyPhysProp);
            Component.Destroy(grabbablePlayerItem.spawnPrefab.GetComponentByName("PhysicsProp"));
            //-0.115 0.56 0.02
            visScript.itemProperties.itemName = "Shrink ray";
            visScript.itemProperties.name = "Shrink ray";
            visScript.itemProperties.rotationOffset = new Vector3(90, 90, 0);
            visScript.itemProperties.positionOffset = new Vector3(-0.115f, 0.56f, 0.02f);
            visScript.grabbable = true;
            visScript.useCooldown = 2f;
            visScript.grabbableToEnemies = true;

            Plugin.log("TRYING TO ADD ASSET TO THING: 1");
            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(shrinkRayItem.spawnPrefab);
            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(grabbablePlayerItem.spawnPrefab);
            grabbablePlayerPrefab = grabbablePlayerItem.spawnPrefab;
            Plugin.log("TRYING TO ADD ASSET TO THING: 2");
            TerminalNode nightNode = new TerminalNode();
            nightNode.displayText = "Shrink ray \nA fun, lightweight toy that the Company repurposed to help employees squeeze through tight spots. Despite it's childish appearance, it really works!";
            Plugin.log("TRYING TO ADD ASSET TO THING: 3");
            Items.RegisterShopItem(shrinkRayItem, null, null, nightNode, shrinkRayItem.creditsWorth);
            
            Plugin.log("TRYING TO ADD ASSET TO THING: 4");
        }

        List<GameObject> grabbablePlayerObjects = new List<GameObject>();
        
        //broadcasts the clientID associated with each GrabblablePlayerObject
        public void BroadcastGrabbedPlayerObjectsList()
        {
            List<(ulong networkId, ulong clientId)> networkClientMap = new List<(ulong networkId, ulong client)>();
            if (grabbablePlayerObjects!.Count > 0)
            {
                foreach(GameObject obj in grabbablePlayerObjects)
                {
                    ulong networkId = obj.GetComponent<NetworkObject>().NetworkObjectId;
                    ulong clientId = obj.GetComponent<GrabbablePlayerObject>().grabbedPlayer.playerClientId;
                    networkClientMap.Add((networkId, clientId));
                }
                string strMap = TupleListToString(networkClientMap);
                Network.Broadcast("OnListTransmit", strMap);
            }
        }
        //Initializes uninitialized GrabbablePlayerObjects for clients
        public void UpdateGrabbablePlayerObjectsClient(List<(ulong networkId, ulong clientId)> tuple)
        {
            //where T1 is the network id
            //and T2 is the client id
            GrabbablePlayerObject[] grabbableObjects = GameObject.FindObjectsOfType<GrabbablePlayerObject>();

            // Filter based on the name
            List<GameObject> filteredObjects = new List<GameObject>();

            foreach (GrabbablePlayerObject grabbableObject in grabbableObjects)
            {
                    filteredObjects.Add(grabbableObject.gameObject);
            }

            if(filteredObjects.Count == 0)
            {
                Plugin.log("ZERO?????", Plugin.LogType.Error);
            }
            foreach (GameObject gpo in filteredObjects)
            {
                ulong gpoNetworkObjId = gpo.GetComponent<NetworkObject>().NetworkObjectId;
                foreach((ulong networkId, ulong clientId) item in tuple){
                    if(item.networkId == gpoNetworkObjId)
                    {
                        Plugin.log("\t"+ item.networkId + ", "+ item.clientId);
                        PlayerControllerB pcb = GetPlayerObject(item.clientId).GetComponent<PlayerControllerB>();
                        gpo.GetComponent<GrabbablePlayerObject>().Initialize(pcb);
                    }
                }
            }
        }
        //Runs whenever player count changes,
        //if you're the host, then delete all grabbablePlayerObjects and make new ones(should maybe just delete the one that leaves or make the one that joins)
        //if you're a client, then delete all grabbablePlayerObjects and make new ones, then ask the host to help it initialize properly
        public void AddGrabbablePlayerItem()
        {
            //grabbablePlayerObjects = new List<GameObject>(newGrabbablePlayerObjects);
            //newGrabbablePlayerObjects.Clear();
            foreach (GameObject obj in grabbablePlayerObjects)
            {
                obj.GetComponent<NetworkObject>().Despawn();
                //GameObject.Destroy(obj);
            }
            grabbablePlayerObjects.Clear();
            //for each player, make a new GrabbablePlayerObject
           
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                //for each player, create and initialize a grabbable player object, these will be networked, and should spawn for other players
                foreach (GameObject player in players)
                {
                    var newObject = UnityEngine.Object.Instantiate(grabbablePlayerPrefab);
                    newObject.GetComponent<NetworkObject>().Spawn();
                    GrabbablePlayerObject gpo = newObject.GetComponent<GrabbablePlayerObject>();
                    gpo.Initialize(player.GetComponent<PlayerControllerB>());
                    grabbablePlayerObjects.Add(newObject);
                }
            }
            else if(NetworkManager.Singleton.IsClient){
                //otherwise, if we're a client that has connected, then we will have all the objects already, but they won't be initialized, so we'll just need to do that
                //beg the server for a list of objects, hopefully they respond??
                Network.Broadcast("OnListRequest");
                //then on receive, we'll run UpdateGrabbablePlayers
            }

        }

        public void ShrinkPlayer(GameObject msgObject, float msgShrinkage, ulong playerID)
        {
            //Todo Make this NOT awful and terrible
            Plugin.log("OKAY HERE IS THE OBJECT TAG BELOW THIS LINE!!!!");
            Plugin.log("Object tag is " + msgObject.tag);
            Plugin.log("The client Id is: " + clientId.ToString());
            Plugin.log("The object name is: " + msgObject.name);

            if (msgObject.tag == "Player")
            {
                Plugin.log("Looks like it must be a player");
                //if the name is just player with not parenthesis, and we're player 0, use playerShrinkAnimation
                if (!(msgObject.name.Contains("(")) && clientId == 0)
                {
                    Plugin.log("Looks like it must be player 0(Us)");
                    //TODO: REPLACE WITH STORED REFERENCE
                    PlayerShrinkAnimation(msgShrinkage, msgObject, GameObject.Find("ScavengerHelmet").GetComponent<Transform>());
                }
                //if the name isn't player, find out what player it is, extract the number, and then compare it with our client id to see if we're being shrunk
                else if (playerID == clientId)
                {
                    Plugin.log("Looks like it must be us!!!!");
                    //TODO: REPLACE WITH STORED REFERENCE
                    PlayerShrinkAnimation(msgShrinkage, msgObject, GameObject.Find("ScavengerHelmet").GetComponent<Transform>());
                }
                //if it's anyone or anything else, we don't care, just use ObjectShrink
                else
                {
                    Plugin.log("Looks like it must be some random person....boring...");
                    ObjectShrinkAnimation(msgShrinkage, msgObject);
                }
            }
            //TODO: ADD NON-PLAYER SHRINKING
        }

        public float GetPlayerScale()
        {
            return myScale;
        }

        public static bool isCurrentPlayerShrunk()
        {
            if (!StartOfRound.Instance.localPlayerController)
                return false;

            return isShrunk(StartOfRound.Instance.localPlayerController.gameObject);
        }
        public static bool isShrunk(GameObject playerObject)
        {
            return playerObject.transform.localScale.x < 1f;
        }

        public void SetPlayerPitch(float pitch, ulong playerID)
        {
            coroutines.SetPlayerPitch.StartRoutine(playerID, pitch);
        }

        public static GameObject GetPlayerObject(ulong playerID)
        {
            // Implement your logic to get the player object
            // For example, you could use an array or a dictionary to store player objects
            // and retrieve them based on playerObjNum
            // GameObject playerObject = ...

            string myPlayerObjectName = "Player";
            if (playerID != 0ul)
            {
                myPlayerObjectName = "Player (" + playerID.ToString() + ")";
            }
            //TODO: REPLACE WITH STORED REFERENCE
            GameObject myPlayerObject = GameObject.Find(myPlayerObjectName);
            return myPlayerObject;
        }

        public void SussifyVents(EnemyVent[] vents)
        {
            if (!ModConfig.Instance.values.canUseVents)
            {
                Plugin.log("Sussification of vents disabled.");
                return;
            }

            Plugin.log("SUSSIFYING VENTS");

            GameObject dungeonEntrance = GameObject.Find("EntranceTeleportA(Clone)");
            MeshRenderer[] renderers = new MeshRenderer[vents.Length];
            for (int i = 0; i < vents.Length; i++)
            {
                Plugin.log("SUSSIFYING VENT " + i);

                GameObject vent = vents[i].gameObject.transform.Find("Hinge").gameObject.transform.Find("VentCover").gameObject;
                renderers[i] = vent.GetComponent<MeshRenderer>();
                vent.tag = "InteractTrigger";
                vent.layer = LayerMask.NameToLayer("InteractableObject");
                var ventTeleport = vents[i].gameObject.AddComponent<VentTeleport>(); // pr-todo: changed this.gameObject to vents[i].gameObject. Will it work?
                var trigger = vent.AddComponent<InteractTrigger>();
                vent.AddComponent<BoxCollider>();

                trigger.hoverIcon = GameObject.Find("StartGameLever")?.GetComponent<InteractTrigger>()?.hoverIcon;
                trigger.hoverTip = "Enter : [LMB]";
                trigger.interactable = true;
                trigger.oneHandedItemAllowed = true;
                trigger.twoHandedItemAllowed = true;
                trigger.holdInteraction = true;
                trigger.timeToHold = 1.5f;
                trigger.timeToHoldSpeedMultiplier = 1f;

                // Create new instances of InteractEvent for each trigger
                trigger.holdingInteractEvent = new InteractEventFloat();
                trigger.onInteract = new InteractEvent();
                trigger.onInteractEarly = new InteractEvent();
                trigger.onStopInteract = new InteractEvent();
                trigger.onCancelAnimation = new InteractEvent();

                EnemyVent siblingVent;
                //checks that we don't set a vent to have itself as a sibling if their is an odd number
                int siblingIndex = vents.Length - i - 1;
                if (siblingIndex == i)
                {
                    System.Random rnd = new System.Random();
                    siblingIndex = rnd.Next(0, vents.Length);
                }
                siblingVent = vents[siblingIndex];
                Plugin.log("\tPairing with vent " + siblingIndex);

                trigger.onInteract.AddListener((player) => ventTeleport.TeleportPlayer(player, siblingVent));
                trigger.enabled = true;
                vent.GetComponent<Renderer>().enabled = true;
                Plugin.log("VentCover Object: " + vent.name);
                Plugin.log("VentCover Renderer Enabled: " + vent.GetComponent<Renderer>().enabled);
                Plugin.log("Hover Icon: " + (trigger.hoverIcon != null ? trigger.hoverIcon.name : "null"));
            }
            coroutines.RenderVents.StartRoutine(dungeonEntrance, renderers); // pr-todo: will dungeonEntrance really work here?
        }

        public void Update()
        {
            if (!GameNetworkManagerPatch.isGameInitialized || !GameNetworkManager.Instance.localPlayerController)
            {
                players.Clear();
                return;
            }

            //If vents exist
            if (RoundManager.Instance.allEnemyVents != null && RoundManager.Instance.allEnemyVents.Length != 0 && sussification == false)
            {
                //sussify vents(add interact trigger)
                SussifyVents(RoundManager.Instance.allEnemyVents);
                sussification = true;
            }

            foreach (GrabbableObject obj in alteredGrabbedItems)
            {
                //Plugin.log(obj.name);
            }
            


            //if our player count changes and on first run, try to update our list of players
            //Plugin.log("SKRAAAAAA");
            //Plugin.log("Connected players: " + GameNetworkManager.Instance.connectedPlayers);
            if (players.Count != StartOfRound.Instance.ClientPlayerList.Count)
            {
                Plugin.log(players.Count.ToString());
                Plugin.log(GameNetworkManager.Instance.connectedPlayers.ToString());
                //cigarette
                Plugin.log("\n a,  8a\r\n `8, `8)                            ,adPPRg,\r\n  8)  ]8                        ,ad888888888b\r\n ,8' ,8'                    ,gPPR888888888888\r\n,8' ,8'                 ,ad8\"\"   `Y888888888P\r\n8)  8)              ,ad8\"\"        (8888888\"\"\r\n8,  8,          ,ad8\"\"            d888\"\"\r\n`8, `8,     ,ad8\"\"            ,ad8\"\"\r\n `8, `\" ,ad8\"\"            ,ad8\"\"\r\n    ,gPPR8b           ,ad8\"\"\r\n   dP:::::Yb      ,ad8\"\"\r\n   8):::::(8  ,ad8\"\"\r\n   Yb:;;;:d888\"\"  Yummy\r\n    \"8ggg8P\"      Nummy");
                Plugin.log("Detected miscounted players, trying to update");
                players.Clear();

                try
                {
                    foreach (PlayerControllerB playerScript in StartOfRound.Instance.allPlayerScripts)
                    {
                        if (playerScript.isPlayerControlled == true)
                        {
                            players.Add(playerScript.gameObject);
                        }
                    }
                }
                catch (Exception e)
                {
                    Plugin.log("Error while adding player. Message: " + e.Message, Plugin.LogType.Error);
                    players.Clear();
                }
   //Place things that should run after a player joins or leaves here vVVVVvvVVVVv
                //re-enable renderers for all vent covers
                MeshRenderer renderer = GameObject.Find("VentEntrance").gameObject.transform.Find("Hinge").gameObject.transform.Find("VentCover").gameObject.GetComponentsInChildren<MeshRenderer>()[0];
                renderer.enabled = true;

                //Self explains, plus I put a million comments around this function
                AddGrabbablePlayerItem();
            }

            if (isCurrentPlayerShrunk())
                CheckIfPlayerAbove();

            foreach (GameObject player in players)
            {
                //TODO: REPLACE WITH OBJECT REFERENCE
                PlayerControllerB playerController = player.GetComponent<PlayerControllerB>();
                if (playerController == null)
                {
                    Plugin.log("playerController is fucking null goddamnit", Plugin.LogType.Warning);
                }
                if (playerController.isHoldingObject == true)
                {
                    GrabbableObject heldObject = playerController.currentlyHeldObjectServer;
                    if (heldObject == null)
                    {
                        Plugin.log("HELD OBJECT IS NULL", Plugin.LogType.Warning);
                        heldObject = playerController.currentlyHeldObject;
                        if (heldObject == null)
                        {
                            Plugin.log("FUCK WHAT THE HELL", Plugin.LogType.Warning);
                        }
                    }

                    bool isInList = false;
                    foreach (String item in ScreenBlockingItems)
                    {
                        if (isInList == false)
                        {
                            if (item.Equals(heldObject.name))
                            {
                                isInList = true;
                            }
                        }
                    }

                    if (!hasIDInList(heldObject.itemProperties.itemId, alteredGrabbedItems) && isInList)
                    {
                        alteredGrabbedItems.Add(heldObject);
                        //TODO: REPLACE WITH OBJECT REFERENCE
                        float scale = player.GetComponent<Transform>().localScale.x;
                        float y = 0f;
                        float z = 0f;
                        float x = 0f;
                        if (!player.gameObject.name.Contains(GameNetworkManager.Instance.localPlayerController.gameObject.name))
                        {
                            x = testVector.x;
                            y = testVector.y;
                            z = testVector.z;
                            y = -0.42f * scale + 0.42f;
                            z = 0f;
                            x = 0.8f * scale - 0.8f;
                            Plugin.log("we is not da client");
                        }
                        else
                        {
                            y = 0.3f * scale - 0.3f;
                            z = -1.44f * scale + 1.44f;
                            x = 0.3f * scale - 0.3f;
                            Plugin.log("we IS da client");
                        }

                        //inverted even though my math was perfect but okay

                        Vector3 posOffsetVect = new Vector3(-x, -y, -z);


                        coroutines.TranslateRelativeOffset.StartRoutine(playerController.playerEye, heldObject, posOffsetVect);
                        //First person engine offset is -0.5099 0.7197 -0.1828 with these numbas

                        //Third person offset should be 0.2099 0.5197 -0.1828
                        //at least on the engine it should be...
                    }
                }
            }

            //Remove the item from the list of altered items and reset them if they're not being held
            foreach (GrabbableObject obj in alteredGrabbedItems)
            {
                if (!obj.isHeld)
                {
                    Plugin.log("removing held object!!! from the list!!!!!");
                    obj.itemProperties.positionOffset = new Vector3(0, 0, 0);
                    alteredGrabbedItems.Remove(obj);
                }
            }

            if (clientId == 239 && GameNetworkManager.Instance.localPlayerController != null)
            {

                clientId = GameNetworkManager.Instance.localPlayerController.playerClientId;
                //Plugin.log("Instance: " + GameNetworkManager.Instance.ToString());
                //mPlugin.log("ClientID: " + clientId.ToString());
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
                Plugin.log(player.GetComponent<PlayerControllerB>().drunkness);
            }*/

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
                        Plugin.log("Player transform got!");
                    }
                    player1Object = GameObject.Find("Player (1)");
                    if (player1Object != null)
                    {
                        player1Transform = player1Object.GetComponent<Transform>();
                    }
                }
                catch (Exception e)
                {
                    Plugin.log("Error in Update(): " + e.Message);
                }
            }

            //mls.LogInfo("\n\n\n\n\n\n HELP \n\n\n\n\n\n");
            
        }
        public Vector3 testVector = new Vector3();
        private bool sussification = false;
        private bool isGrabbableAdded = false;

        public Vector3 getTestVector() { return testVector; }
        private void testOffset(Vector3 posOffsetVect)
        {
            testVector = posOffsetVect;
            coroutines.TranslateRelativeOffset.StartRoutine(StartOfRound.Instance.allPlayerScripts[0].playerEye, StartOfRound.Instance.allPlayerScripts[0].currentlyHeldObjectServer, posOffsetVect);
        }

        public void updatePitch()
        {
            for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
            {
                //String pPlayer = "Player (" + i.ToString() + ")";
                if (StartOfRound.Instance.allPlayerScripts[i] != null)
                {
                    Plugin.log("Altering player voice pitches");
                    SetPlayerPitch(1f, (ulong)i);
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
            int endIndex = shrinkObject.ToString().LastIndexOf('(') - 1;
            string playerObjName = shrinkObject.ToString().Substring(0, endIndex);
            Plugin.log("Sending message that an object is shrinking! Object: \"" + playerObjName + "\" Shrinkage: " + shrinkage);

            Network.Broadcast("OnShrinking", new ShrinkData() { playerObjName = playerObjName, shrinkage = shrinkage });
        }

        //object shrink animation infrastructure!
        public void ObjectShrinkAnimation(float shrinkAmt, GameObject playerObj)
        {
            Plugin.log("LOOKS GOOD SENDING IT TO THE COROUTINE!!!!!", Plugin.LogType.Warning);
            coroutines.ObjectShrinkAnimation.StartRoutine(playerObj, shrinkAmt);
        }

        //Player Shrink animation, shrinks a player over a sinusoidal curve for a duration. Requires the player and mask transforms.
        public void PlayerShrinkAnimation(float shrinkAmt, GameObject playerObj, Transform maskTransform)
        {
            coroutines.PlayerShrinkAnimation.StartRoutine(playerObj, shrinkAmt, maskTransform);
        }
    }
}
