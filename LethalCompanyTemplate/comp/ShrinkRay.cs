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
using static LCShrinkRay.helper.PlayerModification;

namespace LCShrinkRay.comp
{
    public class ShrinkRay : GrabbableObject
    {
        #region Properties
        public const string itemname = "Shrink Ray";

        private const float beamSearchDistance = 10f;

        private PlayerControllerB previousPlayerHeldBy;
        private List<ulong> handledRayHits = new List<ulong>();

        private static GameObject networkPrefab { get; set; }

        private bool IsOnCooldown = false;
        #endregion

        #region Networking
        public static void LoadAsset(AssetBundle assetBundle)
        {
            if (networkPrefab != null) return; // Already loaded

            var assetItem = assetBundle.LoadAsset<Item>("ShrinkRayItem.asset");
            if(assetItem == null )
            {
                Plugin.Log("ShrinkRayItem.asset not found!", Plugin.LogType.Error);
                return;
            }

            networkPrefab = assetItem.spawnPrefab;
            assetItem.creditsWorth = ModConfig.Instance.values.shrinkRayCost;
            assetItem.weight = 1.05f;
            assetItem.canBeGrabbedBeforeGameStart = true;
            networkPrefab.transform.localScale = new Vector3(1f, 1f, 1f);

            ShrinkRay visScript = networkPrefab.AddComponent<ShrinkRay>();

            // Add the FX component for controlling beam fx
            ShrinkRayFX shrinkRayFX = networkPrefab.AddComponent<ShrinkRayFX>();

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
            visScript.useCooldown = 0.5f;
            visScript.grabbableToEnemies = true;
            visScript.itemProperties.syncUseFunction = true;
            visScript.fallTime = 0f;

            NetworkManager.Singleton.AddNetworkPrefab(networkPrefab);

            var terminalNode = ScriptableObject.CreateInstance<TerminalNode>();
            terminalNode.displayText = itemname + "\nA fun, lightweight toy that the Company repurposed to help employees squeeze through tight spots. Despite it's childish appearance, it really works!";
            Items.RegisterShopItem(assetItem, null, null, terminalNode, assetItem.creditsWorth);
        }
        #endregion

        #region Base Methods
        public override void Start()
        {
            base.Start();
            this.itemProperties.requiresBattery = false;
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            if (IsOnCooldown) return;

            try
            {
                Plugin.Log("Triggering " + itemname);
                base.ItemActivate(used, buttonDown);

                ShootRay(ModificationType.Shrinking);
            }
            catch (Exception e) {
                Plugin.Log("Error while shooting ray: " + e.Message, Plugin.LogType.Error);
                Plugin.Log($"Stack Trace: {e.StackTrace}");
            }
        }

        public override void Update()
        {
            base.Update();

            if (isPocketed || IsOnCooldown)
                return;

            if(Mouse.current.middleButton.wasPressedThisFrame) // todo: make middle mouse button scroll through modificationTypes later on, with visible: Mouse.current.scroll.ReadValue().y
                ShootRay(ModificationType.Enlarging);
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
            base.DiscardItem();
        }
        #endregion

        #region Shooting
        //do a cool raygun effect, ray gun sound, cast a ray, and shrink any players caught in the ray
        private void ShootRay(ModificationType type)
        {
           if (playerHeldBy == null || PlayerInfo.CurrentPlayerID != playerHeldBy.playerClientId || playerHeldBy.isClimbingLadder)
                return;

            Plugin.Log("shootingggggg");

            handledRayHits.Clear();

            var transform = this.playerHeldBy.gameplayCamera.transform;
            var ray = new Ray(transform.position, transform.position + transform.forward * beamSearchDistance);
            
            handledRayHits.Clear();
            Plugin.Log("playersMask: " + StartOfRound.Instance.playersMask);
            var layers = ToInt([Mask.Player, Mask.Props, Mask.InteractableObject, Mask.Enemies]);
            var raycastHits = Physics.SphereCastAll(ray, 5f, beamSearchDistance, layers, QueryTriggerInteraction.Collide);
            foreach (var hit in raycastHits)
            {
                if (Vector3.Dot(transform.forward, hit.transform.position - transform.position) < 0f)
                    continue; // Is behind us

                // Check if in line of sight
                if (Physics.Linecast(transform.position, hit.transform.position, out RaycastHit hitInfo, StartOfRound.Instance.collidersRoomDefaultAndFoliage, QueryTriggerInteraction.Ignore))
                {
                    Plugin.Log("\"" + hitInfo.collider.name + "\" [Layer " + hitInfo.collider.gameObject.layer + "] is between us and \"" + hit.collider.name + "\" [Layer " + hit.collider.gameObject.layer + "]");
                    continue;
                }

                if (OnRayHit(hit, type)) // Only single hits for now
                    break;
            }
        }
        
        private bool OnRayHit(RaycastHit hit, ModificationType type)
        {
            var layer = hit.collider.gameObject.layer;

            switch ((Mask)layer)
            {
                case Mask.Player:
                    {
                        if (!hit.transform.TryGetComponent(out PlayerControllerB targetPlayer) || handledRayHits.Contains(targetPlayer.playerClientId))
                            return false;

                        handledRayHits.Add(targetPlayer.playerClientId);
                        Plugin.Log("Ray has hit a PLAYER -> " + targetPlayer.name);
                        if(targetPlayer.playerClientId == playerHeldBy.playerClientId || !CanApplyModificationTo(targetPlayer, type))
                        {
                            Plugin.Log("... but would do nothing.");
                            return false;
                        }

                        OnPlayerModificationServerRpc(this.playerHeldBy.playerClientId, targetPlayer.playerClientId, type);
                        return true;
                    }
                case Mask.Props:
                    {
                        if (!hit.transform.TryGetComponent(out GrabbableObject item))
                            return false;

                        if(item is GrabbablePlayerObject)
                            return false;

                        Plugin.Log("Ray has hit an ITEM -> " + item.name);
                        //Plugin.Log("WIP");
                        return false;
                    }
                case Mask.InteractableObject:
                    {
                        if (!hit.transform.TryGetComponent(out Item item))
                            return false;

                        Plugin.Log("Ray has hit an INTERACTABLE OBJECT -> " + item.name);
                        //Plugin.Log("WIP");
                        return false;
                    }
                case Mask.Enemies:
                    {
                        if (!hit.transform.TryGetComponent(out EnemyAI enemyAI))
                            return false;

                        Plugin.Log("Ray has hit an ENEMY -> \"" + hit.collider.name);
                        //Plugin.Log("WIP");
                        return false;
                    }
                default:
                    Plugin.Log("Ray has hit an unhandled object named \"" + hit.collider.name + "\" [Layer " + layer + "]");
                    return false;
            };

        }
        #endregion

        #region PlayerTargeting
        // ------ Ray hitting Player ------
        [ServerRpc(RequireOwnership = false)]
        public void OnPlayerModificationServerRpc(ulong holderPlayerID, ulong targetPlayerID, ModificationType type)
        {
            Plugin.Log("Player (" + PlayerInfo.CurrentPlayerID + ") modified Player(" + targetPlayerID + "): " + type.ToString());
            OnPlayerModificationClientRpc(holderPlayerID, targetPlayerID, type );
        }

        [ClientRpc]
        public void OnPlayerModificationClientRpc(ulong holderPlayerID, ulong targetPlayerID, ModificationType type)
        {
            Plugin.Log("OnPlayerModificationClientRpc");
            var targetPlayer = PlayerInfo.ControllerFromID(targetPlayerID);
            if (targetPlayer == null) return;

            if (targetPlayer == null || targetPlayer.gameObject == null || targetPlayer.gameObject.transform == null)
            {
                Plugin.Log("Ay.. that's not a valid player somehow..");
                return;
            }

            // For other clients

            var targetingUs = targetPlayer.playerClientId == PlayerInfo.CurrentPlayerID;

            IsOnCooldown = true;
            bool appliedModification = ApplyModificationTo(targetPlayer, type, () =>
            {
                Plugin.Log("Finished ray shoot with type: " + type.ToString());
                IsOnCooldown = false;
            });

            if (appliedModification)
            {
                Plugin.Log("Ray has hit " + (targetingUs ? "us" : "Player (" + targetPlayer.playerClientId + ")") + "!");

                // Shoot ray
                if (transform.TryGetComponent(out ShrinkRayFX shrinkRayFX) && shrinkRayFX != null)
                {
                    var holder = playerHeldBy != null ? playerHeldBy : PlayerInfo.ControllerFromID(holderPlayerID);
                    if(holder != null)
                        shrinkRayFX.RenderRayBeam(holder.gameplayCamera.transform, targetPlayer.transform, type);
                    else
                        Plugin.Log("Unable to determine holder of shrink ray.", Plugin.LogType.Error);
                }
            }
        }
        #endregion

        #region ObjectTargeting
        // ------ Ray hitting Object ------
        public static void OnObjectModification(GameObject targetObject)
        {
            // wip
        }
        #endregion
    }
}
