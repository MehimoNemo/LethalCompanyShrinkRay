using BepInEx.Logging;
using GameNetcodeStuff;
using System;
using UnityEngine;
using Unity.Netcode;
using LC_API.Networking;
using LethalLib.Modules;
using System.IO;
using System.Reflection;
using LCShrinkRay.Config;
using LCShrinkRay.helper;
using static LCShrinkRay.comp.Shrinking;
using System.Runtime.InteropServices;
using System.ComponentModel;
using static LCShrinkRay.comp.ShrinkRay;

namespace LCShrinkRay.comp
{
    internal class ShrinkRay : GrabbableObject
    {
        public const string itemname = "Shrink Ray";

        private PlayerControllerB previousPlayerHeldBy;
        GameObject beamObject;
        LineRenderer lineRenderer;

        public Material beamMaterial;
        public float beamWidth = 0.1f;
        public float beamLength = 10f;
        public float beamDuration = 2f;
        //private Color beamColor = Color.blue;

        public static GameObject grabbablePlayerPrefab;
        internal class HitObjectData
        {
            public string objectName { get; set; }
            public float newSize { get; set; }
        }


        public override void Start()
        {
            base.Start();
            this.itemProperties.requiresBattery = false;
            this.useCooldown = 0.5f;
            Plugin.log("STARTING SHRINKRAY");

            beamMaterial = new Material(Shader.Find("HDRP/Unlit"));
            /*// Set the emission color
            beamMaterial.SetColor("_EmissionColor", Color.blue);
            // Enable emission
            beamMaterial.EnableKeyword("_EMISSION");*/
            Texture2D blueTexture = new Texture2D(1, 1);
            blueTexture.SetPixel(0, 0, Color.blue);
            blueTexture.Apply();
            if (beamMaterial == null)
            {
                Plugin.log("FUCKER DAMNIT SHIT ASS", Plugin.LogType.Error);
            }
            //beamMaterial.mainTexture = blueTexture;
            //beamMaterial.color = beamColor;
            
        }

        public static void AddToGame()
        {
            Plugin.log("Addin " + itemname);
            string assetDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "shrinkasset");
            AssetBundle UpgradeAssets = AssetBundle.LoadFromFile(assetDir);

            //Lethal Company_Data
            Item shrinkRayItem = UpgradeAssets.LoadAsset<Item>("ShrinkRayItem.asset");
            //I SWEAR TO GOD IF THE PROBLEM WAS A LOWERCASE G I WILL KILL ALL OF MANKIND
            Item grabbablePlayerItem = UpgradeAssets.LoadAsset<Item>("grabbablePlayerItem.asset");
            if (grabbablePlayerItem == null)
            {
                Plugin.log("\n\nFUCK WHY IS IT NULL???\n\n");
            }

            shrinkRayItem.creditsWorth = 0; // ModConfig.Instance.values.shrinkRayCost
            shrinkRayItem.weight = 1.05f;
            shrinkRayItem.canBeGrabbedBeforeGameStart = ModConfig.debugMode;

            shrinkRayItem.spawnPrefab.transform.localScale = new Vector3(1f, 1f, 1f);
            ShrinkRay visScript = shrinkRayItem.spawnPrefab.AddComponent<ShrinkRay>();
            GrabbablePlayerObject grabbyScript = grabbablePlayerItem.spawnPrefab.AddComponent<GrabbablePlayerObject>();
            PhysicsProp grabbyPhysProp = shrinkRayItem.spawnPrefab.GetComponent<PhysicsProp>();
            grabbyScript.itemProperties = grabbyPhysProp.itemProperties;


            visScript.itemProperties = shrinkRayItem;
            grabbyScript.itemProperties = grabbablePlayerItem;
            if (grabbyScript.itemProperties == null)
            {
                Plugin.log("\n\nSHIT HOW IS IT NULL???\n\n");
            }
            PhysicsProp.Destroy(grabbyPhysProp);
            UnityEngine.Component.Destroy(grabbablePlayerItem.spawnPrefab.GetComponent<PhysicsProp>());
            //-0.115 0.56 0.02
            grabbyScript.itemProperties.isConductiveMetal = false;
            visScript.itemProperties.itemName = itemname;
            visScript.itemProperties.name = itemname;
            visScript.itemProperties.rotationOffset = new Vector3(90, 90, 0);
            visScript.itemProperties.positionOffset = new Vector3(-0.115f, 0.56f, 0.02f);
            visScript.grabbable = true;
            visScript.useCooldown = 2f;
            visScript.grabbableToEnemies = true;
            //visScript.itemProperties.syncUseFunction = true;

            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(shrinkRayItem.spawnPrefab);
            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(grabbablePlayerItem.spawnPrefab);
            grabbablePlayerPrefab = grabbablePlayerItem.spawnPrefab;
            TerminalNode nightNode = new TerminalNode();
            nightNode.displayText = itemname + "\nA fun, lightweight toy that the Company repurposed to help employees squeeze through tight spots. Despite it's childish appearance, it really works!";
            Items.RegisterShopItem(shrinkRayItem, null, null, nightNode, shrinkRayItem.creditsWorth);
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            try
            {
                Plugin.log("Triggering " + itemname);
                base.ItemActivate(used, buttonDown);

                if (beamObject == null || beamObject.gameObject == null)
                    ShootRayAndSync();
            }
            catch (Exception e) {
                Plugin.log("Error while shooting ray: " + e.Message);
            }
        }

        public float duration = 0.6f;
        private float elapsedTime = 0f;

        public Color startColor = Color.blue;
        public Color endColor = Color.cyan;

        public override void Update()
        {
            base.Update();

            //if beam exists
            try
            { // todo: move to coroutine
                if (beamObject != null && lineRenderer != null && this.playerHeldBy != null && this.playerHeldBy.gameplayCamera != null)
                {
                    Transform transform = this.playerHeldBy.gameplayCamera.transform;
                    Vector3 beamStartPos;
                    Vector3 forward;

                    beamStartPos = transform.position - transform.up * 0.1f;
                    forward = transform.forward;
                    forward = forward * beamLength + beamStartPos;

                    //offset the ding dang beam a lil to the right 
                    beamStartPos += transform.right * 0.35f;
                    forward += transform.right * 0.35f;

                    //offset the beam a lil bit forwards
                    beamStartPos += transform.forward * 1.3f;
                    forward += transform.forward * 1.3f;


                    // Increment the elapsed time based on the frame time
                    elapsedTime += Time.deltaTime;

                    // Calculate the interpolation factor between 0 and 1 based on elapsed time
                    float t = Mathf.Repeat(elapsedTime / duration, 1.0f);
                    float t2 = Mathf.Repeat(elapsedTime + 1f / duration, 1.0f);
                    // Lerp between startColor and endColor
                    Color lerpedColor = Color.Lerp(startColor, endColor, t);
                    Color lerpedColor2 = Color.Lerp(startColor, endColor, t2);

                    // Apply the color to the material or any other component that has color
                    {
                        lineRenderer.endColor = lerpedColor;
                        lineRenderer.startColor = lerpedColor2;
                    }

                    /*Ray ray = new Ray(beamStartPos, forward);
                    RaycastHit hit;
                    float maxRayDistance = beamLength; // Adjust as needed
                    bool hitSomething = Physics.Raycast(ray, out hit, maxRayDistance, StartOfRound.Instance.walkableSurfacesMask);
                    if (hitSomething)
                    {
                        forward -= (hit.distance+1) * transform.forward;
                    }*/

                    lineRenderer.SetPosition(0, beamStartPos);
                    lineRenderer.SetPosition(1, forward);
                }
            }
            catch (Exception e)
            {
                Plugin.log("Error in ShrinkRay.Update(): " + e.Message);
            }
        }

        public void ShootRayAndSync()
        {
            var transform = playerHeldBy.gameplayCamera.transform;
            
            var beamStartPos = transform.position - transform.up * 0.1f;
            var forward = transform.forward;
            forward = forward * beamLength + beamStartPos;

            //offset the ding dang beam a lil to the right 
            beamStartPos += transform.right * 0.35f;
            forward += transform.right * 0.35f;

            //offset the beam a lil bit forwards
            beamStartPos += transform.forward * 1.3f;
            forward += transform.forward * 1.3f;

            Plugin.log("Calling shoot gun....");
            ShootRay(beamStartPos, forward);
        }

        //do a cool raygun effect, ray gun sound, cast a ray, and shrink any players caught in the ray
        private void ShootRay(Vector3 beamStartPos, Vector3 forward)
        {
            Plugin.log("shootingggggg");
            RenderCoolBeam(beamStartPos, beamStartPos + forward * beamLength);

            if (PlayerHelper.currentPlayer().playerClientId == playerHeldBy.playerClientId)
            {
                var enemyColliders = new RaycastHit[10];

                Ray ray = new Ray(beamStartPos, beamStartPos + forward * beamLength);

                int hitEnemiesCount = Physics.SphereCastNonAlloc(ray, 5f, enemyColliders, beamLength, StartOfRound.Instance.playersMask, QueryTriggerInteraction.Collide);
                Plugin.log("Casted Ray");
                Plugin.log("hitEnemiesCount: " + hitEnemiesCount);
                for (int i = 0; i < hitEnemiesCount; i++)
                {
                    Plugin.log("enemycolliderpint: " + enemyColliders[i].point);
                    if (Physics.Linecast(beamStartPos, enemyColliders[i].point, out var hitInfo, StartOfRound.Instance.playersMask, QueryTriggerInteraction.Ignore))
                    {
                        Debug.DrawRay(hitInfo.point, Vector3.up, Color.red, 15f);
                        Debug.DrawLine(beamStartPos, enemyColliders[i].point, Color.cyan, 15f);
                        Plugin.log("Raycast hit wall :c");
                    }
                    else
                        OnRayHit(enemyColliders[i]);
                }
            }
        }

        private void OnRayHit(RaycastHit hit)
        {
            if (hit.transform.TryGetComponent<PlayerControllerB>(out PlayerControllerB component))
            {
                Plugin.log($"Ray has hit player " + component.playerClientId);
                if (component.transform.localScale.x == 1f && component.playerClientId != this.playerHeldBy.playerClientId)
                {
                    OnRayHitPlayer(component);
                    Network.Broadcast("OnRayHitPlayerSync", new PlayerHitData() { playerID = component.playerClientId, modificationType = ModificationType.Shrinking });
                }
            }
            else
            {
                Plugin.log("Could not get hittable script from collider, transform: " + hit.transform.name);
                Plugin.log("collider: " + hit.collider.name);
            }
        }
        internal enum ModificationType
        {
            Normalizing,
            Shrinking,
            Growing
        }

        // ------ Ray hitting Player ------
        public static float NextShrunkenSizeOf(GameObject targetObject)
        {
            return 0.4f;
        }

        public static float NextIncreasedSizeOf(GameObject targetObject)
        {
            return 1f; // wip
        }

        internal class PlayerHitData
        {
            public ulong playerID { get; set; }
            public ModificationType modificationType { get; set; }
        }

        [NetworkMessage("OnRayHitPlayerSync")]
        public static void OnRayHitPlayerSync(ulong sender, PlayerHitData targetPlayerData)
        {
            var targetPlayer = PlayerHelper.GetPlayerController(targetPlayerData.playerID);
            if (targetPlayer == null)
                return;

            OnRayHitPlayer(targetPlayer, targetPlayerData.modificationType);
        }

        public static void OnRayHitPlayer(PlayerControllerB targetPlayer, ModificationType modificationType = ModificationType.Shrinking)
        {
            var targetingUs = targetPlayer.playerClientId == PlayerHelper.currentPlayer().playerClientId;
            Plugin.log("Ray has hit " + (targetingUs ? "us" : "Player (" + targetPlayer.playerClientId + ")") + "!");

            switch (modificationType)
            {
                case ModificationType.Normalizing: case ModificationType.Shrinking: case ModificationType.Growing: // todo: seperate ModificationType.Normalizing
                    {
                    var newSize = modificationType == ModificationType.Shrinking ? NextShrunkenSizeOf(targetPlayer.gameObject) : NextIncreasedSizeOf(targetPlayer.gameObject);
                    if (newSize == targetPlayer.gameObject.transform.localScale.x)
                            return; // Well, nothing changed..

                    if (targetingUs)
                        coroutines.PlayerShrinkAnimation.StartRoutine(targetPlayer.gameObject, newSize, GameObject.Find("ScavengerHelmet").GetComponent<Transform>());
                    else
                        coroutines.ObjectShrinkAnimation.StartRoutine(targetPlayer.gameObject, newSize);

                    if (NetworkManager.Singleton.IsServer)
                    {
                        if(modificationType == ModificationType.Shrinking) // todo: make this if() safer for future cases
                            GrabbablePlayerList.setPlayerGrabbable(targetPlayer.gameObject);
                        // todo: remove grabbable if >= 1f
                    }

                    break;
                }
                default:
                    break;
            }
        }

        //Player Shrink animation, shrinks a player over a sinusoidal curve for a duration. Requires the player and mask transforms.
        public static void changeCurrentPlayerSize(float shrinkAmt, GameObject playerObj, Transform maskTransform)
        {
        }

        // ------ Ray hitting Object ------
        public static void OnRayHitObject(GameObject targetObject)
        {
            // wip
        }

        public void RenderCoolBeam(Vector3 beamStartPos, Vector3 forward)
        {
            Plugin.log("trying to render cool beam");
            Plugin.log("parent is: " + parentObject.gameObject.name);

            try
            {
                if (parentObject.transform.Find("Beam") == null && beamMaterial != null)
                {
                    Plugin.log("trying to create beam object");
                    beamObject = new GameObject("Beam");
                    Plugin.log("Before creating LineRenderer");
                    lineRenderer = beamObject.AddComponent<LineRenderer>();
                    Plugin.log("After creating LineRenderer");
                    lineRenderer.material = beamMaterial;
                    lineRenderer.startWidth = beamWidth;
                    lineRenderer.endWidth = beamWidth * 16;
                    lineRenderer.endColor = new Color(0, 0.5f, 0.5f, 0.5f);
                    lineRenderer.material.renderQueue = 2500; // Adjust as needed

                    Plugin.log("Adding line renderer");
                    lineRenderer.SetPosition(0, beamStartPos);
                    lineRenderer.SetPosition(1, forward);
                    lineRenderer.enabled = true;
                    lineRenderer.numCapVertices = 6;
                    lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    lineRenderer.receiveShadows = false;

                    Destroy(beamObject, beamDuration);
                }
            }
            catch(Exception e)
            {
                Plugin.log("Rendern't.. maybe it was " + e.Message);
            }
        }

        public override void EquipItem()
        {
            base.EquipItem();
            previousPlayerHeldBy = playerHeldBy;
            previousPlayerHeldBy.equippedUsableItemQE = true;
        }
        public override void PocketItem()
        {
            base.PocketItem();
        }

        public override void DiscardItem()
        {
            Plugin.log("Discarding");
            base.DiscardItem();
        }
    }

}
