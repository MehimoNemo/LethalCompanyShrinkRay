using HarmonyLib;
using UnityEngine.InputSystem;
using GameNetcodeStuff;
using LittleCompany.helper;
using LittleCompany.components;
using UnityEngine;
using static LittleCompany.helper.Moons;
using Unity.Netcode;
using System.Collections;
using LittleCompany.modifications;
using static LittleCompany.modifications.Modification;
using static LittleCompany.helper.EnemyInfo;
using UnityEngine.SceneManagement;
using System.Linq;
using LethalLib.Modules;

namespace LittleCompany.patches
{
    [HarmonyPatch]
    internal class DebugPatches
    {
#if DEBUG
        private static int itemIndex = Random.Range(0, 20);

        public const string ImperiumReferenceChain = "giosuel.Imperium";

        private static bool? _ImperiumEnabled;

        public static bool ImperiumEnabled
        {
            get
            {
                if (_ImperiumEnabled == null)
                    _ImperiumEnabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(ImperiumReferenceChain);

                return _ImperiumEnabled.GetValueOrDefault(false);
            }
        }

        [HarmonyPatch(typeof(SceneManager), "LoadScene", [typeof(string)])]
        [HarmonyPrefix]
        public static void LoadScenePrefix(ref string sceneName)
        {
            if (sceneName == "ColdOpen1")
                sceneName = "MainMenu";
        }

        /*[HarmonyPatch(typeof(StartOfRound), "LoadShipGrabbableItems")]
        [HarmonyPrefix]
        public static void LoadShipGrabbableItems()
        {
            int[] array = ES3.Load<int[]>("shipGrabbableItemIDs", GameNetworkManager.Instance.currentSaveFileName);
            foreach (var item in array)
                Plugin.Log("item saved with id: " + item);
        }*/

        private static bool Executing = false;

        [HarmonyPatch(typeof(PlayerControllerB), "Update")]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.First)]
        public static void OnUpdate()
        {
            if (Executing) return;

            if(CheckFunctionKeys())
                GameNetworkManager.Instance?.StartCoroutine(WaitAfterKeyPress());
        }

        public static IEnumerator WaitAfterKeyPress()
        {
            if (Executing) yield break;

            Executing = true;
            yield return new WaitForSeconds(0.2f);
            Executing = false;
        }

        public static bool CheckFunctionKeys()
        {
            if (!ImperiumEnabled && Keyboard.current.f1Key.wasPressedThisFrame)
            {
                SpawnEnemyInFrontOfPlayer(PlayerInfo.CurrentPlayer, Enemy.ForestGiant);
            }

            else if (!ImperiumEnabled && Keyboard.current.f2Key.wasPressedThisFrame)
            {
                ApplyModification(ModificationType.Shrinking);
            }

            else if (!ImperiumEnabled && Keyboard.current.f3Key.wasPressedThisFrame)
            {
                ApplyModification(ModificationType.Enlarging);
            }

            else if (!ImperiumEnabled && Keyboard.current.f4Key.wasPressedThisFrame)
            {
                var enemyType = EnemyTypeByEnum(Enemy.ForestGiant);
                if (enemyType != null)
                {
                    var location = PlayerInfo.CurrentPlayer.transform.position + PlayerInfo.CurrentPlayer.transform.forward * 60f;
                    SpawnEnemyAt(location, 0f, enemyType);
                }

                //StartOfRound.Instance.ManuallyEjectPlayersServerRpc();
            }

            else if (!ImperiumEnabled && Keyboard.current.f5Key.wasPressedThisFrame)
            {
                SpawnItemInFront(LittleShrinkingPotion.NetworkPrefab);
            }

            else if (!ImperiumEnabled && Keyboard.current.f6Key.wasPressedThisFrame)
            {
                SpawnItemInFront(LittleEnlargingPotion.NetworkPrefab);
            }

            else if (Keyboard.current.f7Key.wasPressedThisFrame)
            {
                SpawnItemInFront(ShrinkRay.networkPrefab);
            }

            else if (Keyboard.current.f8Key.wasPressedThisFrame)
            {
                SpawnItemInFront(ItemInfo.itemByType(ItemInfo.VanillaItem.Laser_pointer));
            }

            else if (Keyboard.current.f9Key.wasPressedThisFrame)
            {
                SpawnVehicleInFront();
            }

            else if (Keyboard.current.f10Key.wasPressedThisFrame)
            {
                foreach(var door in Resources.FindObjectsOfTypeAll<DoorLock>())
                {
                    if (door != null && door.isLocked)
                        PlayerInfo.CurrentPlayer.TeleportPlayer(door.lockPickerPosition.position);
                }
                //TeleportIntoShip();
            }

            else if (Keyboard.current.f11Key.wasPressedThisFrame)
            {
                TeleportOutsideDungeon();
            }

            else if (Keyboard.current.f12Key.wasPressedThisFrame)
            {
                TeleportInsideDungeon();
            }

            else
                return false;

            return true;
        }
        #region Methods
        public static GameObject CreateCube(Color color, Transform parent = null)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            if(parent != null)
                cube.transform.SetParent(parent, false);

            if (cube.TryGetComponent(out BoxCollider boxCollider))
                boxCollider.enabled = false;

            if (cube.TryGetComponent(out MeshRenderer meshRenderer))
            {
                meshRenderer.material = color.a < 1f ? Materials.Glass : new Material(Shader.Find("HDRP/Lit"));
                meshRenderer.material.color = color;
                meshRenderer.enabled = true;
            }

            return cube;
        }

        public static void SpawnVehicleInFront()
        {
            var terminal = Resources.FindObjectsOfTypeAll<Terminal>().First();
            if (terminal != null && terminal.buyableVehicles.Length > 0 && terminal.buyableVehicles.First()?.vehiclePrefab != null)
            {
                var vehicle = Object.Instantiate(terminal.buyableVehicles.First().vehiclePrefab, RoundManager.Instance.VehiclesContainer);
                vehicle.transform.position = PlayerInfo.CurrentPlayer.transform.position + PlayerInfo.CurrentPlayer.transform.forward * 10f + Vector3.up * 5f;
                vehicle.GetComponent<NetworkObject>()?.Spawn();

                if (terminal.buyableVehicles.First().secondaryPrefab != null)
                {
                    var vehicleSec = Object.Instantiate(terminal.buyableVehicles.First().secondaryPrefab, RoundManager.Instance.VehiclesContainer);
                    vehicleSec.transform.position = PlayerInfo.CurrentPlayer.transform.position + PlayerInfo.CurrentPlayer.transform.forward * 10f + Vector3.up * 5f;
                    vehicleSec.GetComponent<NetworkObject>()?.Spawn();
                }

                if (vehicle.TryGetComponent(out VehicleController vehicleController))
                {
                    Plugin.Log("Disable vehicle controller");
                    vehicleController.enabled = false;
                    vehicleController.StartCoroutine(EnableVehicleControllerLater(vehicleController));
                }
            }
        }

        private static IEnumerator EnableVehicleControllerLater(VehicleController vehicleController)
        {
            yield return new WaitForSeconds(1f);
            vehicleController.enabled = true;
        }

        public static void SpawnItemInFront(Item item)
        {
            if (item == null) return;

            SpawnItemInFront(item.spawnPrefab);
        }

        public static void SpawnItemInFront(GameObject networkPrefab)
        {
            if (!PlayerInfo.IsHost)
            {
                Plugin.Log("That's a host-only debug feature.", Plugin.LogType.Error);
                return;
            }

            if (networkPrefab == null)
            {
                Plugin.Log("Unable to spawn item. networkPrefab was null.", Plugin.LogType.Error);
                return;
            }

            var item = Object.Instantiate(networkPrefab);
            Object.DontDestroyOnLoad(item);
            item.GetComponent<NetworkObject>()?.Spawn();
            item.transform.position = PlayerInfo.CurrentPlayer.transform.position + PlayerInfo.CurrentPlayer.transform.forward * 1.5f;
            if (item.TryGetComponent(out GrabbableObject grabbableObject))
            {
                grabbableObject.itemProperties.canBeGrabbedBeforeGameStart = true;
                /*if (grabbableObject.itemProperties.requiresBattery)
                    grabbableObject.insertedBattery = new Battery(isEmpty: false, 1f); */
            }
        }

        public static void SpawnNextItemInFront()
        {
            SpawnItemInFront(ItemInfo.SpawnableItems[itemIndex++].spawnPrefab);
            itemIndex %= ItemInfo.SpawnableItems.Count;
        }

        public static void LogCurrentLevelEnemyNames()
        {
            var enemies = CurrentLevelEnemyNames;
            Plugin.Log(enemies != null ? enemies.Join(null, "\n") : "Not in a round.");
        }

        public static void SpawnTurretInFrontOfPlayer(PlayerControllerB targetPlayer)
        {
            foreach (var obj in RoundManager.Instance.currentLevel.spawnableMapObjects)
            {
                if (obj.prefabToSpawn.GetComponentInChildren<Turret>() == null) continue;

                var position = targetPlayer.transform.position + targetPlayer.transform.forward * 3f;
                var turret = Object.Instantiate(obj.prefabToSpawn, position, Quaternion.identity, RoundManager.Instance.mapPropsContainer.transform);
                turret.transform.position = position;
                turret.transform.forward = new Vector3(1, 0, 0);
                turret.GetComponent<NetworkObject>().Spawn(true);
                return;
            }
        }

        public static void SpawnEnemyInFrontOfPlayer(PlayerControllerB targetPlayer, Enemy enemy)
        {
            var enemyType = EnemyTypeByEnum(enemy);
            if (enemyType is null)
            {
                Plugin.Log("No enemy found..");
                return;
            }

            var location = targetPlayer.transform.position + targetPlayer.transform.forward * 5f;
            SpawnEnemyAt(location, 0f, enemyType);
        }

        public static void SpawnAnyRandomEnemyInFrontOfPlayer(PlayerControllerB targetPlayer) => SpawnEnemyInFrontOfPlayer(targetPlayer, RandomEnemy);

        public static void LogPosition()
        {
            Plugin.Log("Current position: " + PlayerInfo.CurrentPlayer.gameObject.transform.position + ". Moon: " + RoundManager.Instance.currentLevel.name);
        }

        public static void TeleportIntoShip()
        {
            if (StartOfRound.Instance.inShipPhase || !StartOfRound.Instance.shipHasLanded) return;

            // Teleport inside ship
            PlayerInfo.CurrentPlayer.gameObject.transform.position = new Vector3(2.84f, 0.29f, -14.41f);
        }

        public static void TeleportOutsideDungeon()
        {
            if (StartOfRound.Instance.inShipPhase) return;

            var entrance = GameObject.Find("EntranceTeleportA")?.GetComponent<EntranceTeleport>();
            if (entrance == null) return;

            PlayerInfo.CurrentPlayer.TeleportPlayer(entrance.entrancePoint.position);
        }

        public static void TeleportInsideDungeon()
        {
            if (StartOfRound.Instance.inShipPhase) return;

            var entrance = GameObject.Find("EntranceTeleportA")?.GetComponent<EntranceTeleport>();
            if (entrance == null) return;

            PlayerInfo.CurrentPlayer.TeleportPlayer(entrance.exitPoint.position);
        }

        public static void ReloadAllSounds()
        {
            GameNetworkManager.Instance.StartCoroutine(AssetLoader.LoadAudioAsync("shrinkRayGrab.wav", (item) => ShrinkRay.grabSFX = item));
            GameNetworkManager.Instance.StartCoroutine(AssetLoader.LoadAudioAsync("shrinkRayDrop.wav", (item) => ShrinkRay.dropSFX = item));
            GameNetworkManager.Instance.StartCoroutine(AssetLoader.LoadAudioAsync("shrinkRayLoad.wav", (item) => ShrinkRay.loadSFX = item));
            GameNetworkManager.Instance.StartCoroutine(AssetLoader.LoadAudioAsync("shrinkRayUnload.wav", (item) => ShrinkRay.unloadSFX = item));
            GameNetworkManager.Instance.StartCoroutine(AssetLoader.LoadAudioAsync("shrinkRayNoTarget.wav", (item) => ShrinkRay.noTargetSFX = item));

            GameNetworkManager.Instance.StartCoroutine(AssetLoader.LoadAudioAsync("shrinkRayBeam.wav", (item) => ShrinkRayFX.beamSFX = item));

            GameNetworkManager.Instance.StartCoroutine(AssetLoader.LoadAudioAsync("deathPoof.wav", (item) => deathPoofSFX = item));

            GameNetworkManager.Instance.StartCoroutine(AssetLoader.LoadAudioAsync("playerGrab.wav", (item) => GrabbablePlayerObject.grabSFX = item));
            GameNetworkManager.Instance.StartCoroutine(AssetLoader.LoadAudioAsync("playerDrop.wav", (item) => GrabbablePlayerObject.dropSFX = item));
            GameNetworkManager.Instance.StartCoroutine(AssetLoader.LoadAudioAsync("playerThrow.wav", (item) => GrabbablePlayerObject.throwSFX = item));

            GameNetworkManager.Instance.StartCoroutine(AssetLoader.LoadAudioAsync("potionGrab.wav", (item) => LittlePotion.grabSFX = item));
            GameNetworkManager.Instance.StartCoroutine(AssetLoader.LoadAudioAsync("potionDrop.wav", (item) => LittlePotion.dropSFX = item));
            GameNetworkManager.Instance.StartCoroutine(AssetLoader.LoadAudioAsync("potionConsume.wav", (item) => LittlePotion.consumeSFX = item));
            GameNetworkManager.Instance.StartCoroutine(AssetLoader.LoadAudioAsync("potionNoConsume.wav", (item) => LittlePotion.noConsumeSFX = item));
        }

        public static void ApplyModification(ModificationType type)
        {
            if (!PlayerModification.CanApplyModificationTo(PlayerInfo.CurrentPlayer, type, PlayerInfo.CurrentPlayer))
                return;

            Executing = true;
            PlayerModification.ApplyModificationTo(PlayerInfo.CurrentPlayer, type, PlayerInfo.CurrentPlayer, 1f, () => Executing = false);
        }
        #endregion
#endif
    }
}
