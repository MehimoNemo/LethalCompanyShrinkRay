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
using UnityEngine.InputSystem;
using System.Collections.Generic;

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
            visScript.itemProperties.toolTips = ["Shrink: LMB", "Enlarge: MMB"];
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
                    ShootRayAndSync(ModificationType.Shrinking);
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

            if (isPocketed)
                return;

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

            if(Mouse.current.middleButton.wasPressedThisFrame) // todo: make middle mouse button scroll through modificationTypes later on, with visible: Mouse.current.scroll.ReadValue().y
            {
                if (beamObject == null || beamObject.gameObject == null)
                {
                    ShootRayAndSync(ModificationType.Enlarging);
                }
            }
        }

        public void ShootRayAndSync(ModificationType type)
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
            ShootRay(beamStartPos, forward, type);
        }

        //do a cool raygun effect, ray gun sound, cast a ray, and shrink any players caught in the ray
        private void ShootRay(Vector3 beamStartPos, Vector3 forward, ModificationType type)
        {
            Plugin.log("shootingggggg");
            RenderRayBeam(beamStartPos, beamStartPos + forward * beamLength, type);

            if (PlayerHelper.currentPlayer().playerClientId != playerHeldBy.playerClientId)
                return;

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
                    OnRayHit(enemyColliders[i], type);
            }
        }

        private void RenderRayBeam(Vector3 beamStartPos, Vector3 forward, ModificationType type)
        {
            Plugin.log("trying to render cool beam. parent is: " + parentObject.gameObject.name);
            try
            {
                if (parentObject.transform.Find("Beam") != null || beamMaterial == null)
                    return;

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
                lineRenderer.SetPosition(0, beamStartPos);
                lineRenderer.SetPosition(1, forward);
                lineRenderer.enabled = true;
                lineRenderer.numCapVertices = 6;
                lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lineRenderer.receiveShadows = false;
                Plugin.log("Done with rendering beam");

                Destroy(beamObject, beamDuration);
            }
            catch (Exception e)
            {
                Plugin.log("Rendern't.. maybe it was " + e.Message);
            }
        }

        private void OnRayHit(RaycastHit hit, ModificationType type)
        {
            if (hit.transform.TryGetComponent<PlayerControllerB>(out PlayerControllerB component))
            {
                Plugin.log($"Ray has hit player " + component.playerClientId);
                if (component.playerClientId != this.playerHeldBy.playerClientId)
                {
                    OnPlayerModification(component, type);
                    Network.Broadcast("OnPlayerModificationSync", new PlayerModificationData() { playerID = component.playerClientId, modificationType = type });
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
            Enlarging
        }

        internal static readonly List<float> possiblePlayerSizes = [0f, 0.4f, 1f, 1.3f, 1.6f];

        // ------ Ray hitting Player ------

        public static float NextShrunkenSizeOf(GameObject targetObject)
        {
            if (!ModConfig.Instance.values.multipleShrinking)
                return possiblePlayerSizes[1];

            var currentPlayerSize = targetObject.transform.localScale.x;
            var currentSizeIndex = possiblePlayerSizes.IndexOf(currentPlayerSize);
            if (currentSizeIndex <= 0)
                return currentPlayerSize;

            return possiblePlayerSizes[currentSizeIndex-1];
        }

        public static float NextIncreasedSizeOf(GameObject targetObject)
        {
            var currentPlayerSize = targetObject.transform.localScale.x;
            var currentSizeIndex = possiblePlayerSizes.IndexOf(currentPlayerSize);
            if (currentSizeIndex == -1 || currentPlayerSize == possiblePlayerSizes.Count - 1)
                return currentPlayerSize;

            if(currentSizeIndex > 2) // remove this if() once we think about growing
                return possiblePlayerSizes[2];

            return possiblePlayerSizes[currentSizeIndex + 1];
        }

        internal class PlayerModificationData
        {
            public ulong playerID { get; set; }
            public ModificationType modificationType { get; set; }
        }

        [NetworkMessage("OnPlayerModificationSync")]
        public static void OnPlayerModificationSync(ulong sender, PlayerModificationData modificationData)
        {
            Plugin.log("Player (" + sender + ") modified Player(" + modificationData.playerID + "): " + modificationData.modificationType.ToString());
            var targetPlayer = PlayerHelper.GetPlayerController(modificationData.playerID);
            if (targetPlayer == null)
                return;

            OnPlayerModification(targetPlayer, modificationData.modificationType );
        }

        public static void OnPlayerModification(PlayerControllerB targetPlayer, ModificationType type)
        {
            var targetingUs = targetPlayer.playerClientId == PlayerHelper.currentPlayer().playerClientId;
            Plugin.log("Ray has hit " + (targetingUs ? "us" : "Player (" + targetPlayer.playerClientId + ")") + "!");

            switch (type)
            {
                case ModificationType.Normalizing:
                    {
                        Plugin.log("Normalizing..");
                        var newSize = 1f;
                        if (newSize != targetPlayer.gameObject.transform.localScale.x)
                        {
                            Plugin.log("Raytype: " + type.ToString() + ". New size: " + newSize);
                            if (targetingUs)
                                coroutines.PlayerShrinkAnimation.StartRoutine(targetPlayer.gameObject, newSize, GameObject.Find("ScavengerHelmet").GetComponent<Transform>());
                            else
                                coroutines.ObjectShrinkAnimation.StartRoutine(targetPlayer.gameObject, newSize);
                        }

                        GrabbablePlayerList.RemovePlayerGrabbableIfExists(targetPlayer);

                        if (targetingUs)
                            Vents.unsussifyAll();
                        break;
                    }

                case ModificationType.Shrinking:
                    {
                        var newSize = NextShrunkenSizeOf(targetPlayer.gameObject);
                        Plugin.log("Shrinking to size " + newSize);
                        if (newSize == targetPlayer.gameObject.transform.localScale.x)
                            return; // Well, nothing changed..

                        if (newSize <= 0 && targetPlayer.AllowPlayerDeath())
                            return; // Can't shrink players to death in ship phase

                        Plugin.log("Raytype: " + type.ToString() + ". New size: " + newSize);
                        if (targetingUs)
                            coroutines.PlayerShrinkAnimation.StartRoutine(targetPlayer.gameObject, newSize, GameObject.Find("ScavengerHelmet").GetComponent<Transform>(), () =>
                            {
                                if (newSize <= 0f)
                                    targetPlayer.KillPlayer(Vector3.down, false, CauseOfDeath.Crushing);
                            });
                        else
                            coroutines.ObjectShrinkAnimation.StartRoutine(targetPlayer.gameObject, newSize);

                        if (PlayerHelper.isHost()) // todo: create a mechanism that only allows larger players to grab small ones
                            GrabbablePlayerList.SetPlayerGrabbable(targetPlayer);

                        if (targetingUs)
                            Vents.SussifyAll();

                        break;
                    }

                case ModificationType.Enlarging:
                    {
                        var newSize = NextIncreasedSizeOf(targetPlayer.gameObject);
                        Plugin.log("Enlarging to size " + newSize);
                        if (newSize == targetPlayer.gameObject.transform.localScale.x)
                            return; // Well, nothing changed..

                        Plugin.log("Raytype: " + type.ToString() + ". New size: " + newSize);
                        if (targetingUs)
                            coroutines.PlayerShrinkAnimation.StartRoutine(targetPlayer.gameObject, newSize, GameObject.Find("ScavengerHelmet").GetComponent<Transform>());
                        else
                            coroutines.ObjectShrinkAnimation.StartRoutine(targetPlayer.gameObject, newSize);

                        if (newSize >= 1f) // todo: create a mechanism that only allows larger players to grab small ones
                            GrabbablePlayerList.RemovePlayerGrabbableIfExists(targetPlayer);

                        if (targetingUs)
                            Vents.unsussifyAll();

                        break;
                    }
                default:
                    break;
            }
        }

        // ------ Ray hitting Object ------
        public static void OnObjectModification(GameObject targetObject)
        {
            // wip
        }

        public override void EquipItem()
        {
            // idea: play a fading-in sound, like energy of gun is loading
            base.EquipItem();
            previousPlayerHeldBy = playerHeldBy;
            previousPlayerHeldBy.equippedUsableItemQE = true;
        }
        public override void PocketItem()
        {
            // idea: play a fading-out sound
            base.PocketItem();
        }

        public override void DiscardItem()
        {
            Plugin.log("Discarding");
            base.DiscardItem();
        }
    }

}
