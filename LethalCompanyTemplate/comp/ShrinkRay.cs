using GameNetcodeStuff;
using System;
using UnityEngine;
using Unity.Netcode;
using LethalLib.Modules;
using LCShrinkRay.Config;
using LCShrinkRay.helper;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using LCShrinkRay.patches;

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

        public static GameObject shrinkRayFXPrefab;
        public ShrinkRayFX shrinkRayFX;
        //private Color beamColor = Color.blue;
        private List<ulong> handledRayHits = new List<ulong>();

        public static GameObject networkPrefab { get; set; }

        public static void LoadAsset(AssetBundle assetBundle)
        {
            if (networkPrefab != null) return; // Already loaded

            var assetItem = assetBundle.LoadAsset<Item>("ShrinkRayItem.asset");
            if(assetItem == null )
            {
                Plugin.log("ShrinkRayItem.asset not found!", Plugin.LogType.Error);
                return;
            }

            networkPrefab = assetItem.spawnPrefab;
            assetItem.creditsWorth = 0; // ModConfig.Instance.values.shrinkRayCost
            assetItem.weight = 1.05f;
            assetItem.canBeGrabbedBeforeGameStart = ModConfig.debugMode;
            networkPrefab.transform.localScale = new Vector3(1f, 1f, 1f);

            ShrinkRay visScript = networkPrefab.AddComponent<ShrinkRay>();
            //GrabbablePlayerList.Instance = networkPrefab.AddComponent<GrabbablePlayerList>();
            shrinkRayItem.spawnPrefab.AddComponent<ShrinkRayFX>();

			// Load ShrinkRayFX AssetBundle -- The name of the unity gameobject (prefabbed) is "Shrink Ray VFX"
            string FXAssetDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "fxasset");
            AssetBundle FXAssets = AssetBundle.LoadFromFile(FXAssetDir);
            shrinkRayFXPrefab = FXAssets.LoadAsset<GameObject>("Shrink Ray VFX");
            if (shrinkRayFXPrefab == null)
            {
                Plugin.log("AssetBundle Loading Error: Shrink Ray VFX", Plugin.LogType.Error);
            }
            
            // Add the FX component
            ShrinkRayFX shrinkRayFX = shrinkRayItem.spawnPrefab.AddComponent<ShrinkRayFX>();
            
            // Customize the ShrinkRayFX (I just found some good settings by tweaking in game. Easier done here than in the prefab, which is why I made properties on the script)
            shrinkRayFX.noiseSpeed = 5;
            shrinkRayFX.noisePower = 0.1f;
            
            
            // END ShrinkRay Setup

            Destroy(networkPrefab.GetComponent<PhysicsProp>());

            visScript.itemProperties = assetItem;

            //-0.115 0.56 0.02
            visScript.itemProperties = assetItem;
            visScript.itemProperties.itemName = itemname;
            visScript.itemProperties.name = itemname;
            visScript.itemProperties.rotationOffset = new Vector3(90, 90, 0);
            visScript.itemProperties.positionOffset = new Vector3(-0.115f, 0.56f, 0.02f);
            visScript.itemProperties.toolTips = ["Shrink: LMB", "Enlarge: MMB"];
            visScript.grabbable = true;
            visScript.useCooldown = 2f;
            visScript.grabbableToEnemies = true;
            visScript.itemProperties.syncUseFunction = true;

            NetworkManager.Singleton.AddNetworkPrefab(networkPrefab);

            TerminalNode nightNode = new TerminalNode();
            nightNode.displayText = itemname + "\nA fun, lightweight toy that the Company repurposed to help employees squeeze through tight spots. Despite it's childish appearance, it really works!";
            Items.RegisterShopItem(assetItem, null, null, nightNode, assetItem.creditsWorth);
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
                Plugin.log("Error while shooting ray: " + e.Message, Plugin.LogType.Error);
                Plugin.log($"Stack Trace: {e.StackTrace}");
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
                if ((beamObject == null || beamObject.gameObject == null) && this.playerHeldBy != null)
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
            
            // Old call, now render ray beam when it finds a player
            // RenderRayBeam(beamStartPos, beamStartPos + forward * beamLength, type);

            if (PlayerHelper.currentPlayer().playerClientId != playerHeldBy.playerClientId)
                return;

            var enemyColliders = new RaycastHit[10];

            Ray ray = new Ray(beamStartPos, beamStartPos + forward * beamLength);

            int hitEnemiesCount = Physics.SphereCastNonAlloc(ray, 5f, enemyColliders, beamLength, StartOfRound.Instance.playersMask, QueryTriggerInteraction.Collide);
            //Plugin.log("Casted Ray");
            //Plugin.log("hitEnemiesCount: " + hitEnemiesCount);

            handledRayHits.Clear();
            for (int i = 0; i < hitEnemiesCount; i++)
            {
                //Plugin.log("enemycolliderpint: " + enemyColliders[i].point);

                if (Physics.Linecast(beamStartPos, enemyColliders[i].point, out var hitInfo, StartOfRound.Instance.playersMask, QueryTriggerInteraction.Ignore))
                {
                    // Did raycast hit wall?
                    Debug.DrawRay(hitInfo.point, Vector3.up, Color.red, 15f);
                    Debug.DrawLine(beamStartPos, enemyColliders[i].point, Color.cyan, 15f);
                    Plugin.log("Raycast hit wall :c");
                    
                }
                else
                {
                    // Raycast hit player
                    OnRayHit(enemyColliders[i], type);
                }
            }
        }
        
        private void RenderSingleRayBeam(Transform start, Transform target, ModificationType type)
        {
            // Created overload to reduce merge conflicts between both branches, can remove the old method after
            
            Plugin.log("trying to render cool beam. parent is: " + parentObject.gameObject.name);
            try
            {
                if (!shrinkRayFX) shrinkRayFX = transform.GetComponent<ShrinkRayFX>();

                GameObject fxObject = shrinkRayFX.CreateNewBeam(playerHeldBy.gameplayCamera.transform);
                    
                if (!fxObject) Plugin.log("FX Object Null", Plugin.LogType.Error);
                
                Transform bezier1 = fxObject.transform.GetChild(0).Find("Pos1");
                Transform bezier2 = fxObject.transform.GetChild(0).Find("Pos2");
                Transform bezier3 = fxObject.transform.GetChild(0).Find("Pos3");
                Transform bezier4 = fxObject.transform.GetChild(0).Find("Pos4");
                
                if (!bezier1) Plugin.log("bezier1 Null", Plugin.LogType.Error);
                if (!bezier2) Plugin.log("bezier2 Null", Plugin.LogType.Error);
                if (!bezier3) Plugin.log("bezier3 Null", Plugin.LogType.Error);
                if (!bezier4) Plugin.log("bezier4 Null", Plugin.LogType.Error);

                Transform targetHeadTransform = target.gameObject.GetComponent<PlayerControllerB>().gameplayCamera.transform.Find("HUDHelmetPosition").transform;

                // Stole this from above, minor adjustments to where the beam comes from
                Vector3 beamStartPos = (start.position - (transform.up * 0.1f) + (transform.right * 0.35f) + (transform.forward * 1.3f));
                
                // Set bezier 1 (start point)
                bezier1.transform.position = beamStartPos;
                bezier1.transform.SetParent(start, true);
                
                // Set bezier 2 (curve)
                bezier2.transform.position = beamStartPos;
                bezier2.transform.SetParent(start, true);
                
                // Set bezier 3 (curve)
                bezier3.transform.position = (targetHeadTransform.position) + (Vector3.up * shrinkRayFX.bezier3YOffset);
                bezier3.transform.SetParent(targetHeadTransform, true);
                
                // Set Bezier 4 (final endpoint)
                Vector3 beamEndPos = (targetHeadTransform.position) + (Vector3.up * shrinkRayFX.bezier4YOffset); // endpos is targets head adjusted on y axis slightly
                bezier4.transform.position = beamEndPos;
                bezier4.transform.SetParent(targetHeadTransform, true);
                    
                // Destroy the beziers before the fxObject, just barely
                Destroy(bezier1, beamDuration - 0.05f);
                Destroy(bezier2, beamDuration - 0.05f);
                Destroy(bezier3, beamDuration - 0.05f);
                Destroy(bezier4, beamDuration - 0.05f);
                Destroy(fxObject, beamDuration);
            }
            catch (Exception e)
            {
                Plugin.log("error trying to render beam: " + e.Message, Plugin.LogType.Error);
                Plugin.log("error source: " + e.Source);
                Plugin.log("error stack: " + e.StackTrace);
            }
        }
        
        private void OnRayHit(RaycastHit hit, ModificationType type)
        {
            if (hit.transform.TryGetComponent(out PlayerControllerB component))
            {
                Plugin.log($"Ray has hit player " + component.playerClientId);
                if (component.playerClientId != this.playerHeldBy.playerClientId && !handledRayHits.Contains(component.playerClientId))
                {
                    OnPlayerModificationServerRpc(component.playerClientId, type);
                    handledRayHits.Add(component.playerClientId);
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

        internal static readonly List<float> possiblePlayerSizes = new() {0f, 0.4f, 1f, 1.3f, 1.6f};

        // ------ Ray hitting Player ------

        public static float NextShrunkenSizeOf(GameObject targetObject)
        {
            if (!ModConfig.Instance.values.multipleShrinking)
                return possiblePlayerSizes[1];

            var currentPlayerSize = Mathf.Round(targetObject.transform.localScale.x * 100f) / 100f; // round to 2 digits
            var currentSizeIndex = possiblePlayerSizes.IndexOf(currentPlayerSize);
            if (currentSizeIndex <= 0)
                return currentPlayerSize;

            return possiblePlayerSizes[currentSizeIndex-1];
        }

        public static float NextIncreasedSizeOf(GameObject targetObject)
        {
            var currentPlayerSize = Mathf.Round(targetObject.transform.localScale.x * 100f) / 100f; // round to 2 digits
            var currentSizeIndex = possiblePlayerSizes.IndexOf(currentPlayerSize);
            if (currentSizeIndex == -1 || currentPlayerSize == possiblePlayerSizes.Count - 1)
                return currentPlayerSize;

            if(currentSizeIndex >= 2) // remove this if() once we think about growing
                return possiblePlayerSizes[2];

            return possiblePlayerSizes[currentSizeIndex + 1];
        }

        public static void debugOnPlayerModificationWorkaround(PlayerControllerB targetPlayer, ModificationType type)
        {
            Plugin.log("debugOnPlayerModificationWorkaround");
            var sr = new ShrinkRay();
            sr.OnPlayerModificationServerRpc(targetPlayer.playerClientId, type);
            Destroy(sr,4);
        }

        [ServerRpc(RequireOwnership = false)]
        public void OnPlayerModificationServerRpc(ulong targetPlayerID, ModificationType type)
        {
            Plugin.log("OnPlayerModificationServerRpc");
            Plugin.log("Player (" + PlayerHelper.currentPlayer().playerClientId + ") modified Player(" + targetPlayerID + "): " + type.ToString());
            OnPlayerModificationClientRpc(targetPlayerID, type );
        }

        [ClientRpc]
        public void OnPlayerModificationClientRpc(ulong targetPlayerID, ModificationType type)
        {
            var targetPlayer = PlayerHelper.GetPlayerController(targetPlayerID);
            if (targetPlayer == null) return;

            if (targetPlayer == null || targetPlayer.gameObject == null || targetPlayer.gameObject.transform == null)
            {
                Plugin.log("Ay.. that's not a valid player somehow..");
                return;
            }

            var targetingUs = targetPlayer.playerClientId == PlayerHelper.currentPlayer().playerClientId;
            Plugin.log("Ray has hit " + (targetingUs ? "us" : "Player (" + targetPlayer.playerClientId + ")") + "!");

            switch (type)
            {
                case ModificationType.Normalizing:
                    {
                        var newSize = 1f;
                        if (newSize != targetPlayer.gameObject.transform.localScale.x)
                        {
                            Plugin.log("Raytype: " + type.ToString() + ". New size: " + newSize);
                            coroutines.PlayerShrinkAnimation.StartRoutine(targetPlayer, newSize);
                        }

                        if(PlayerHelper.isHost())
                            GrabbablePlayerList.Instance.RemovePlayerGrabbableServerRpc(targetPlayer.playerClientId);

                        if (targetingUs)
                            Vents.unsussifyAll();
                        break;
                    }

                case ModificationType.Shrinking:
                    {
                        var newSize = NextShrunkenSizeOf(targetPlayer.gameObject);
                        if (newSize == targetPlayer.gameObject.transform.localScale.x)
                            return; // Well, nothing changed..

                        if (newSize <= 0 && !targetPlayer.AllowPlayerDeath())
                            return; // Can't shrink players to death in ship phase

                        Plugin.log("Raytype: " + type.ToString() + ". New size: " + newSize);
                        coroutines.PlayerShrinkAnimation.StartRoutine(targetPlayer, newSize, () =>
                        {
                            if (targetingUs && newSize <= 0f)
                                targetPlayer.KillPlayer(Vector3.down, false, CauseOfDeath.Crushing);
                        });

                        if (newSize < 1f && PlayerHelper.isHost()) // todo: create a mechanism that only allows larger players to grab small ones
                        {
                            Plugin.log("About to call SetPlayerGrabbableServerRpc");
                            GrabbablePlayerList.Instance.SetPlayerGrabbableServerRpc(targetPlayer.playerClientId);
                        }
                        else
                        {
                            Plugin.log("NOT HOST!");
                        }

                        if (targetingUs)
                            Vents.SussifyAll();

                        break;
                    }

                case ModificationType.Enlarging:
                    {
                        var newSize = NextIncreasedSizeOf(targetPlayer.gameObject);
                        if (newSize == targetPlayer.gameObject.transform.localScale.x)
                            return; // Well, nothing changed..

                        Plugin.log("Raytype: " + type.ToString() + ". New size: " + newSize);
                        coroutines.PlayerShrinkAnimation.StartRoutine(targetPlayer, newSize);

                        if (newSize >= 1f && PlayerHelper.isHost()) // todo: create a mechanism that only allows larger players to grab small ones
                            GrabbablePlayerList.Instance.RemovePlayerGrabbableServerRpc(targetPlayer.playerClientId);

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
