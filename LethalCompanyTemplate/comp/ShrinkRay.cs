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
using System.IO;
using System.Collections;

namespace LCShrinkRay.comp
{
    public class ShrinkRay : GrabbableObject
    {
        #region Properties
        internal static string BaseAssetPath = Path.Combine(AssetLoader.BaseAssetPath, "Shrink");

        private const float beamSearchDistance = 10f;

        private List<ulong> handledRayHits = new List<ulong>();

        public static GameObject networkPrefab { get; set; }

        private bool IsInUse = false;

        internal AudioSource audioSource;

        internal static AudioClip grabSFX;
        internal static AudioClip dropSFX;

        internal static AudioClip loadSFX;
        internal static AudioClip unloadSFX;
        internal static AudioClip noTargetSFX;
        #endregion

        #region Networking
        public static void LoadAsset()
        {
            if (networkPrefab != null) return; // Already loaded

            var assetItem = AssetLoader.littleCompanyAsset?.LoadAsset<Item>(Path.Combine(BaseAssetPath, "ShrinkRayItem.asset"));
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

            ShrinkRay shrinkRay = networkPrefab.AddComponent<ShrinkRay>();
            GameNetworkManager.Instance.StartCoroutine(AssetLoader.LoadAudioAsync("shrinkRayGrab.wav", (item) => grabSFX = item));
            GameNetworkManager.Instance.StartCoroutine(AssetLoader.LoadAudioAsync("shrinkRayDrop.wav", (item) => dropSFX = item));

            GameNetworkManager.Instance.StartCoroutine(AssetLoader.LoadAudioAsync("shrinkRayLoad.wav", (item) => loadSFX = item));
            GameNetworkManager.Instance.StartCoroutine(AssetLoader.LoadAudioAsync("shrinkRayUnload.wav", (item) => unloadSFX = item));
            GameNetworkManager.Instance.StartCoroutine(AssetLoader.LoadAudioAsync("shrinkRayNoTarget.wav", (item) => noTargetSFX = item));

            // Add the FX component for controlling beam fx
            ShrinkRayFX shrinkRayFX = networkPrefab.AddComponent<ShrinkRayFX>();
            GameNetworkManager.Instance.StartCoroutine(AssetLoader.LoadAudioAsync("shrinkRayBeam.wav", (item) => ShrinkRayFX.beamSFX = item));
            GameNetworkManager.Instance.StartCoroutine(AssetLoader.LoadAudioAsync("deathPoof.wav", (item) => deathPoofSFX = item));

            Destroy(networkPrefab.GetComponent<PhysicsProp>()); // todo: make this not needed

            shrinkRay.itemProperties = assetItem;
            shrinkRay.itemProperties.toolTips = ["Shrink: LMB", "Enlarge: MMB"];
            shrinkRay.grabbable = true;
            shrinkRay.grabbableToEnemies = true;
            shrinkRay.fallTime = 0f;

            NetworkManager.Singleton.AddNetworkPrefab(networkPrefab);

            var terminalNode = ScriptableObject.CreateInstance<TerminalNode>();
            terminalNode.displayText = shrinkRay.name + "\nA fun, lightweight toy that the Company repurposed to help employees squeeze through tight spots. Despite it's childish appearance, it really works!";
            Items.RegisterShopItem(shrinkRay.itemProperties, null, null, terminalNode, shrinkRay.itemProperties.creditsWorth);
        }
        #endregion

        #region Base Methods
        public override void Start()
        {
            base.Start();
            //itemProperties.grabSFX = grabSFX;
            //itemProperties.dropSFX = dropSFX;

            audioSource = GetComponent<AudioSource>();
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            if (IsInUse) return;

            Plugin.Log("Triggering " + name);
            base.ItemActivate(used, buttonDown);

            if (playerHeldBy == null) return; // Shouldn't happen, just for safety
            ShootRayServerRpc(playerHeldBy.playerClientId, ModificationType.Shrinking);
        }

        public override void Update()
        {
            base.Update();

            if (isPocketed || IsInUse)
                return;

            if (Mouse.current.middleButton.wasPressedThisFrame) // todo: make middle mouse button scroll through modificationTypes later on, with visible: Mouse.current.scroll.ReadValue().y
            {
                if (playerHeldBy == null) return; // Shouldn't happen, just for safety
                ShootRayClientRpc(playerHeldBy.playerClientId, ModificationType.Enlarging);
            }
        }

        public override void EquipItem()
        {
            audioSource?.PlayOneShot(grabSFX);
            base.EquipItem();
        }

        public override void PocketItem()
        {
            // idea: play a fading-out sound
            base.PocketItem();
        }

        public override void DiscardItem()
        {
            audioSource?.PlayOneShot(dropSFX);
            base.DiscardItem();
        }

        public override void GrabItem()
        {
            base.GrabItem();
        }
        #endregion

        #region Shooting
        [ServerRpc(RequireOwnership = false)]
        private void ShootRayServerRpc(ulong playerHeldByID, ModificationType mode)
        {
            Plugin.Log("ShootRayServerRpc -> " + itemProperties.syncUseFunction);
            if (IsInUse) return;

            ShootRayClientRpc(playerHeldByID, mode);
        }
        [ClientRpc]
        private void ShootRayClientRpc(ulong playerHeldByID, ModificationType mode)
        {
            Plugin.Log("ShootRayClientRpc");
            if (IsInUse) return;

            IsInUse = true;
            if (playerHeldBy == null)
            {
                Plugin.Log("playerHeldBy not synced!", Plugin.LogType.Warning);
                playerHeldBy = PlayerInfo.ControllerFromID(playerHeldByID);
            }

            StartCoroutine(LoadRay(() => ShootRay(mode)));
        }

        private IEnumerator LoadRay(Action onComplete = null)
        {
            Plugin.Log("LoadRay", Plugin.LogType.Warning);
            if (audioSource != null && loadSFX != null)
            {
                audioSource.PlayOneShot(loadSFX);
                yield return new WaitWhile(() => audioSource.isPlaying);
            }
            else
                Plugin.Log("AudioSource or AudioClip for loading ShrinkRay was null!", Plugin.LogType.Warning);

            Plugin.Log("Loading completed");
            if (onComplete != null)
                onComplete();
        }

        private IEnumerator UnloadRay()
        {
            Plugin.Log("UnloadRay", Plugin.LogType.Warning);
            if (audioSource != null && unloadSFX != null)
            {
                audioSource.PlayOneShot(unloadSFX);
                yield return new WaitWhile(() => audioSource.isPlaying);
            }
            else
                Plugin.Log("AudioSource or AudioClip for unloading ShrinkRay was null!", Plugin.LogType.Warning);

            yield return new WaitForSeconds(useCooldown);

            IsInUse = false;
        }

        private IEnumerator NoTargetForRay()
        {
            Plugin.Log("NoTargetForRay", Plugin.LogType.Warning);
            if (audioSource != null && noTargetSFX != null)
            {
                audioSource.PlayOneShot(noTargetSFX);
                yield return new WaitWhile(() => audioSource.isPlaying);
            }
            else
                Plugin.Log("AudioSource or AudioClip for unloading ShrinkRay was null!", Plugin.LogType.Warning);

            yield return new WaitForSeconds(useCooldown);

            IsInUse = false;
        }

        //do a cool raygun effect, ray gun sound, cast a ray, and shrink any players caught in the ray
        private void ShootRay(ModificationType type)
        {
            if (playerHeldBy == null || playerHeldBy.isClimbingLadder)
            {
                StartCoroutine(NoTargetForRay());
                return;
            }

            Plugin.Log("Shooting ray gun!", Plugin.LogType.Warning);

            handledRayHits.Clear();

            var transform = playerHeldBy.gameplayCamera.transform;
            var ray = new Ray(transform.position, transform.position + transform.forward * beamSearchDistance);

            bool rayHasHit = false;

            var layers = ToInt([Mask.Player, Mask.Props, Mask.InteractableObject, Mask.Enemies]);
            var raycastHits = Physics.SphereCastAll(ray, 5f, beamSearchDistance, layers, QueryTriggerInteraction.Collide); // todo: linecast instead!
            foreach (var hit in raycastHits)
            {
                if (Vector3.Dot(transform.forward, hit.transform.position - transform.position) < 0f)
                    continue; // Is behind us

                // Check if in line of sight
                if (Physics.Linecast(transform.position, hit.transform.position, out RaycastHit hitInfo, StartOfRound.Instance.collidersRoomDefaultAndFoliage, QueryTriggerInteraction.Ignore))
                {
                    if(IsOwner)
                        Plugin.Log("\"" + hitInfo.collider.name + "\" [Layer " + hitInfo.collider.gameObject.layer + "] is between us and \"" + hit.collider.name + "\" [Layer " + hit.collider.gameObject.layer + "]");
                    continue;
                }

                rayHasHit = OnRayHit(hit, type);
                if(rayHasHit) // Only single hits for now
                    break;
            }

            if (!rayHasHit)
                StartCoroutine(NoTargetForRay());
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
                        if(IsOwner)
                            Plugin.Log("Ray has hit a PLAYER -> " + targetPlayer.name);
                        if(targetPlayer.playerClientId == playerHeldBy.playerClientId || !CanApplyModificationTo(targetPlayer, type))
                        {
                            if (IsOwner)
                                Plugin.Log("... but would do nothing.");
                            return false;
                        }

                        if (IsOwner)
                            OnPlayerModificationServerRpc(playerHeldBy.playerClientId, targetPlayer.playerClientId, type);
                        return true;
                    }
                case Mask.Props:
                    {
                        if (!hit.transform.TryGetComponent(out GrabbableObject item))
                            return false;

                        if(item is GrabbablePlayerObject)
                            return false;

                        if (IsOwner)
                            Plugin.Log("Ray has hit an ITEM -> " + item.name);
                        //Plugin.Log("WIP");
                        return false;
                    }
                case Mask.InteractableObject:
                    {
                        if (!hit.transform.TryGetComponent(out Item item))
                            return false;

                        if (IsOwner)
                            Plugin.Log("Ray has hit an INTERACTABLE OBJECT -> " + item.name);
                        //Plugin.Log("WIP");
                        return false;
                    }
                case Mask.Enemies:
                    {
                        if (!hit.transform.TryGetComponent(out EnemyAI enemyAI))
                            return false;

                        if (IsOwner)
                            Plugin.Log("Ray has hit an ENEMY -> \"" + hit.collider.name);
                        //Plugin.Log("WIP");
                        return false;
                    }
                default:
                    if (IsOwner)
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

            bool appliedModification = ApplyModificationTo(targetPlayer, type, () =>
            {
                Plugin.Log("Finished player modification with type: " + type.ToString());
            });

            if (appliedModification)
            {
                Plugin.Log("Ray has hit " + (targetingUs ? "us" : "Player (" + targetPlayer.playerClientId + ")") + "!");

                // Shoot ray
                if (transform.TryGetComponent(out ShrinkRayFX shrinkRayFX) && shrinkRayFX != null)
                {
                    var holder = playerHeldBy != null ? playerHeldBy : PlayerInfo.ControllerFromID(holderPlayerID);
                    if (holder != null)
                    {
                        StartCoroutine(shrinkRayFX.RenderRayBeam(holder.gameplayCamera.transform, targetPlayer.transform, type, audioSource, () =>
                        {
                            Plugin.Log("Ray beam, has finished.");
                            StartCoroutine(UnloadRay());
                        }));
                        return;
                    }
                }

                Plugin.Log("Unable to shoot ray beam.", Plugin.LogType.Error);
                StartCoroutine(UnloadRay());
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
