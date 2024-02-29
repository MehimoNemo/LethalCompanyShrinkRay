using HarmonyLib;
using System;
using UnityEngine.InputSystem;
using GameNetcodeStuff;
using LCShrinkRay.helper;
using LCShrinkRay.comp;
using UnityEngine;
using static LCShrinkRay.helper.Moons;
using Unity.Netcode;

namespace LCShrinkRay.patches
{
    [HarmonyPatch]
    internal class DebugPatches
    {
#if DEBUG
        private static int waitFrames = 0;

        internal static AudioClip consumeSFX = AssetLoader.LoadAudio("potionConsume.wav");

        [HarmonyPatch(typeof(PlayerControllerB), "Update")]
        [HarmonyPostfix]
        public static void OnUpdate(PlayerControllerB __instance)
        {
            if (waitFrames > 0) // Don't execute it multiple times
            {
                waitFrames--;
                return;
            }

            try
            {
                if (Keyboard.current.f1Key.wasPressedThisFrame)
                {
                    //LogInsideEnemyNames();
                    //SpawnEnemyInFrontOfPlayer("Centipede", PlayerInfo.CurrentPlayer);
                    PlayerModification.ApplyModificationTo(PlayerInfo.CurrentPlayer, PlayerModification.ModificationType.Shrinking);
                }

                else if (Keyboard.current.f2Key.wasPressedThisFrame)
                {
                    PlayerModification.ApplyModificationTo(PlayerInfo.CurrentPlayer, PlayerModification.ModificationType.Enlarging);
                }

                else if (Keyboard.current.f3Key.wasPressedThisFrame)
                {
                }

                else if (Keyboard.current.f4Key.wasPressedThisFrame)
                {

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
                    return;

                waitFrames = 5;

            }
            catch (Exception e)
            {
                Plugin.Log("[DebugPatches] Error: " + e.Message, Plugin.LogType.Error);
            }
        }

        #region Methods
        public static GameObject CreateCube(Transform parent, Color color)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(parent);

            if (cube.TryGetComponent(out BoxCollider boxCollider))
                boxCollider.enabled = false;

            if (cube.TryGetComponent(out MeshRenderer meshRenderer))
            {
                meshRenderer.sharedMaterial = new Material(Shader.Find("HDRP/Lit"));
                meshRenderer.sharedMaterial.color = color;
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

        public static void LogInsideEnemyNames()
        {
            string enemyTypes = "";
            RoundManager.Instance.currentLevel.Enemies.ForEach(enemyType => { enemyTypes += " " + enemyType.enemyType.name; });
            Plugin.Log("EnemyTypes:" + enemyTypes); // Centipede SandSpider HoarderBug Flowerman Crawler Blob DressGirl Puffer Nutcracker
        }

        public static void SpawnEnemyInFrontOfPlayer(string enemyName, PlayerControllerB targetPlayer)
        {
            int enemyIndex = RoundManager.Instance.currentLevel.Enemies.FindIndex(spawnableEnemy => spawnableEnemy.enemyType.name == enemyName);
            if (enemyIndex != -1)
            {
                var location = targetPlayer.transform.position + targetPlayer.transform.forward * 3;
                RoundManager.Instance.SpawnEnemyOnServer(location, 0f, enemyIndex);

                // I tried so hard and got so far, but in the end... there's still an errooooorrrrr
            }
        }

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
        #endregion
#endif
    }
}
