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

namespace LittleCompany.patches
{
    [HarmonyPatch]
    internal class DebugPatches
    {
#if DEBUG
        private static bool Executing = false;

        [HarmonyPatch(typeof(PlayerControllerB), "Update")]
        [HarmonyPostfix]
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
            if (Keyboard.current.f1Key.wasPressedThisFrame)
            {
                LogCurrentLevelEnemyNames();
                //SpawnEnemyInFrontOfPlayer(PlayerInfo.CurrentPlayer, Enemy.Slime);
            }

            else if (Keyboard.current.f2Key.wasPressedThisFrame)
            {
                ApplyModification(ModificationType.Shrinking);
            }

            else if (Keyboard.current.f3Key.wasPressedThisFrame)
            {
                ApplyModification(ModificationType.Enlarging);
            }

            else if (Keyboard.current.f4Key.wasPressedThisFrame)
            {
                Plugin.Log(GrabbablePlayerList.Log);
            }

            else if (Keyboard.current.f5Key.wasPressedThisFrame)
            {
                SpawnItemInFront(LittleShrinkingPotion.networkPrefab);
            }

            else if (Keyboard.current.f6Key.wasPressedThisFrame)
            {
                SpawnItemInFront(LittleEnlargingPotion.networkPrefab);
            }

            else if (Keyboard.current.f7Key.wasPressedThisFrame)
            {
                SpawnItemInFront(ShrinkRay.networkPrefab);
            }

            else if (Keyboard.current.f8Key.wasPressedThisFrame)
            {
                ReloadAllSounds();
            }

            else if (Keyboard.current.f9Key.wasPressedThisFrame)
            {
                LogPosition();
            }

            else if (Keyboard.current.f10Key.wasPressedThisFrame)
            {
                TeleportIntoShip();
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

            var item = UnityEngine.Object.Instantiate(networkPrefab);
            UnityEngine.Object.DontDestroyOnLoad(item);
            item.GetComponent<NetworkObject>().Spawn();
            item.transform.position = PlayerInfo.CurrentPlayer.transform.position + PlayerInfo.CurrentPlayer.transform.forward * 1.5f;
        }

        public static void LogCurrentLevelEnemyNames()
        {
            var enemies = CurrentLevelEnemyNames;
            Plugin.Log(enemies != null ? enemies.Join(null, "\n") : "Not in a round.");
        }

        public static int GetEnemyIndex(string enemyName = null, bool inside = true)
        {
            var enemyList = inside ? RoundManager.Instance.currentLevel.Enemies : RoundManager.Instance.currentLevel.OutsideEnemies;
            if (enemyName != null)
            {
                var index = enemyList.FindIndex(spawnableEnemy => spawnableEnemy.enemyType.name == enemyName);
                if (index != -1) return index;
            }

            return UnityEngine.Random.Range(0, enemyList.Count - 1);
        }

        public static void SpawnEnemyInFrontOfPlayer(PlayerControllerB targetPlayer, Enemy? enemy = null)
        {
            var enemyName = enemy.HasValue ? EnemyNameOf(enemy.Value) : "";
            var enemyIndex = GetEnemyIndex(enemyName, targetPlayer.isInsideFactory);
            if (enemyIndex == -1)
            {
                Plugin.Log("No enemy found..");
                return;
            }
            {
                var location = targetPlayer.transform.position + targetPlayer.transform.forward * 3;
                RoundManager.Instance.SpawnEnemyOnServer(location, 0f, enemyIndex);
            }
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

            Vector3 pos;
            switch ((Moon)RoundManager.Instance.currentLevel.levelID)
            {
                case Moon.Experimentation:  pos = new Vector3(-111.04f, 2.97f, -17.62f);    break;
                case Moon.Assurance:        pos = new Vector3(131.96f, 6.52f, 74.69f);      break;
                case Moon.Vow:              pos = new Vector3(-29.41f, -1.15f, 148.34f);    break;
                case Moon.March:            pos = new Vector3(-154.78f, -3.94f, 21.79f);    break;
                case Moon.Rend:             pos = new Vector3(49.29f, -16.78f, -149.28f);   break;
                case Moon.Dine:             pos = new Vector3(157.60f, -15.11f, -41.07f);   break;
                case Moon.Offense:          pos = new Vector3(127.70f, 16.42f, -57.77f);    break;
                case Moon.Titan:            pos = new Vector3(-33.79f, 47.75f, 7.48f);      break;
                default: return;
            }

            PlayerInfo.CurrentPlayer.TeleportPlayer(pos);
        }

        public static void TeleportInsideDungeon()
        {
            if (StartOfRound.Instance.inShipPhase) return;

            Vector3 pos;
            switch ((Moon)RoundManager.Instance.currentLevel.levelID)
            {
                case Moon.Experimentation:  pos = new Vector3(-14.50f, -219.56f, 65.91f);   break;
                case Moon.Assurance:        pos = new Vector3(-5.09f, -219.56f, 65.94f);    break;
                case Moon.Vow:              pos = new Vector3(-29.41f, -1.15f, 148.34f);    break;
                case Moon.March:            pos = new Vector3(-6.03f, -219.56f, 65.92f);    break;
                case Moon.Rend:             pos = new Vector3(-6.70f, -219.54f, 65.83f);    break;
                case Moon.Dine:             pos = new Vector3(-7.22f, -219.56f, 65.90f);    break;
                case Moon.Offense:          pos = new Vector3(-5.60f, -219.56f, 65.92f);    break;
                case Moon.Titan:            pos = new Vector3(-7.22f, -219.56f, 65.90f);    break;
                default: return;
            }

            PlayerInfo.CurrentPlayer.TeleportPlayer(pos);
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
            if (!PlayerModification.CanApplyModificationTo(PlayerInfo.CurrentPlayer, type))
                return;

            Executing = true;
            PlayerModification.ApplyModificationTo(PlayerInfo.CurrentPlayer, type, () => Executing = false);
        }
        #endregion
#endif
    }
}
