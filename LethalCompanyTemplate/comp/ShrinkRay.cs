using GameNetcodeStuff;
using System;
using UnityEngine;
using Unity.Netcode;
using LethalLib.Modules;
using LCShrinkRay.Config;
using LCShrinkRay.helper;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using static LCShrinkRay.helper.LayerMasks;

namespace LCShrinkRay.comp
{
    public class ShrinkRay : GrabbableObject
    {
        #region Properties
        public const string itemname = "Shrink Ray";

        private const float beamSearchDistance = 10f;

        private PlayerControllerB previousPlayerHeldBy;
        private List<ulong> handledRayHits = new List<ulong>();

        public static GameObject networkPrefab { get; set; }

        public enum ModificationType
        {
            Normalizing,
            Shrinking,
            Enlarging
        }

        internal static readonly List<float> possiblePlayerSizes = new() { 0f, 0.4f, 1f, 1.3f, 1.6f };
        #endregion

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
            assetItem.creditsWorth = ModConfig.Instance.values.shrinkRayCost;
            assetItem.weight = 1.05f;
            assetItem.canBeGrabbedBeforeGameStart = ModConfig.debugMode;
            networkPrefab.transform.localScale = new Vector3(1f, 1f, 1f);

            ShrinkRay visScript = networkPrefab.AddComponent<ShrinkRay>();

            // Add the FX component for controlling beam fx
            ShrinkRayFX shrinkRayFX = networkPrefab.AddComponent<ShrinkRayFX>();
            
            // Customize the ShrinkRayFX (I just found some good settings by tweaking in game. Easier done here than in the prefab, which is why I made properties on the script)
            shrinkRayFX.noiseSpeed = 5;
            shrinkRayFX.noisePower = 0.1f;

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

            var terminalNode = ScriptableObject.CreateInstance<TerminalNode>();
            terminalNode.displayText = itemname + "\nA fun, lightweight toy that the Company repurposed to help employees squeeze through tight spots. Despite it's childish appearance, it really works!";
            Items.RegisterShopItem(assetItem, null, null, terminalNode, assetItem.creditsWorth);
        }

        public override void Start()
        {
            base.Start();
            this.itemProperties.requiresBattery = false;
            this.useCooldown = 0.5f;
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            try
            {
                Plugin.log("Triggering " + itemname);
                base.ItemActivate(used, buttonDown);

                ShootRay(ModificationType.Shrinking);
            }
            catch (Exception e) {
                Plugin.log("Error while shooting ray: " + e.Message, Plugin.LogType.Error);
                Plugin.log($"Stack Trace: {e.StackTrace}");
            }
        }

        public override void Update()
        {
            base.Update();

            if (isPocketed)
                return;

            if(Mouse.current.middleButton.wasPressedThisFrame) // todo: make middle mouse button scroll through modificationTypes later on, with visible: Mouse.current.scroll.ReadValue().y
                ShootRay(ModificationType.Enlarging);
        }

        //do a cool raygun effect, ray gun sound, cast a ray, and shrink any players caught in the ray
        private void ShootRay(ModificationType type)
        {
            if (playerHeldBy == null || PlayerHelper.currentPlayer().playerClientId != playerHeldBy.playerClientId)
                return;

            Plugin.log("shootingggggg");

            handledRayHits.Clear();

            var transform = this.playerHeldBy.gameplayCamera.transform;
            var ray = new Ray(transform.position, transform.position + transform.forward * beamSearchDistance);
            
            handledRayHits.Clear();
            Plugin.log("playersMask: " + StartOfRound.Instance.playersMask);
            var layers = toInt([Mask.Player, Mask.Props, Mask.InteractableObject, Mask.Enemies]);
            var raycastHits = Physics.SphereCastAll(ray, 5f, beamSearchDistance, layers, QueryTriggerInteraction.Collide);
            foreach (var hit in raycastHits)
            {
                // Check if in line of sight
                if (Physics.Linecast(transform.position, hit.transform.position, out RaycastHit hitInfo, StartOfRound.Instance.collidersRoomDefaultAndFoliage, QueryTriggerInteraction.Ignore))
                {
                    Plugin.log("\"" + hitInfo.collider.name + "\" [Layer " + hitInfo.collider.gameObject.layer + "] is between us and \"" + hit.collider.name + "\" [Layer " + hit.collider.gameObject.layer + "]");
                    continue;
                }
                
                OnRayHit(hit, type);
            }
        }
        
        private void OnRayHit(RaycastHit hit, ModificationType type)
        {
            var layer = hit.collider.gameObject.layer;

            switch ((Mask)layer)
            {
                case Mask.Player:
                    {
                        if (!hit.transform.TryGetComponent(out PlayerControllerB targetPlayer) || handledRayHits.Contains(targetPlayer.playerClientId))
                            return;

                        handledRayHits.Add(targetPlayer.playerClientId);
                        Plugin.log("Ray has hit a PLAYER -> " + hit.collider.name);
                        if (targetPlayer.playerClientId != this.playerHeldBy.playerClientId)
                            OnPlayerModificationServerRpc(this.playerHeldBy.playerClientId, targetPlayer.playerClientId, type);
                        break;
                    }
                case Mask.Props:
                    {
                        if (!hit.transform.TryGetComponent(out GrabbableObject item))
                            return;

                        Plugin.log("Ray has hit an ITEM -> " + item.name);
                        break;
                    }
                case Mask.InteractableObject:
                    {
                        if (!hit.transform.TryGetComponent(out Item item))
                            return;

                        Plugin.log("Ray has hit an INTERACTABLE OBJECT -> " + item.name);
                        break;
                    }
                case Mask.Enemies:
                    {
                        if (!hit.transform.TryGetComponent(out EnemyAI enemyAI))
                            return;

                        Plugin.log("Ray has hit an ENEMY -> \"" + hit.collider.name);
                        break;
                    }
                default:
                    Plugin.log("Ray has hit an unhandled object named \"" + hit.collider.name + "\" [Layer " + layer + "]");
                    break;
            };
        }

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
            coroutines.PlayerShrinkAnimation.StartRoutine(targetPlayer, type == ModificationType.Shrinking ? NextShrunkenSizeOf(targetPlayer.gameObject) : NextIncreasedSizeOf(targetPlayer.gameObject));
        }

        [ServerRpc(RequireOwnership = false)]
        public void OnPlayerModificationServerRpc(ulong holderPlayerID, ulong targetPlayerID, ModificationType type)
        {
            Plugin.log("Player (" + PlayerHelper.currentPlayer().playerClientId + ") modified Player(" + targetPlayerID + "): " + type.ToString());
            OnPlayerModificationClientRpc(holderPlayerID, targetPlayerID, type );
        }

        [ClientRpc]
        public void OnPlayerModificationClientRpc(ulong holderPlayerID, ulong targetPlayerID, ModificationType type)
        {
            var targetPlayer = PlayerHelper.GetPlayerController(targetPlayerID);
            if (targetPlayer == null) return;

            if (targetPlayer == null || targetPlayer.gameObject == null || targetPlayer.gameObject.transform == null)
            {
                Plugin.log("Ay.. that's not a valid player somehow..");
                return;
            }

            // For other clients
            var holder = this.playerHeldBy != null ? this.playerHeldBy : PlayerHelper.GetPlayerController(holderPlayerID);

            // Shoot ray
            Plugin.log("trying to render cool beam. parent is: " + parentObject.gameObject.name);
            if (transform.TryGetComponent(out ShrinkRayFX shrinkRayFX) && shrinkRayFX != null)
                shrinkRayFX.RenderRayBeam(holder.gameplayCamera.transform, targetPlayer.transform, type);

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
                            {
                                // Poof Target to death because they are too small to exist
                                if (ShrinkRayFX.deathPoofFX != null)
                                {
                                    GameObject deathPoofObject = Instantiate(ShrinkRayFX.deathPoofFX, targetPlayer.transform.position, Quaternion.identity);
                                    Destroy(deathPoofObject, 4f);
                                }

                                targetPlayer.KillPlayer(Vector3.down, false, CauseOfDeath.Crushing);
							}
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
