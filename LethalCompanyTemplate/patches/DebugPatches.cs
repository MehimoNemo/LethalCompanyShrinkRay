using HarmonyLib;
using LC_API.Networking;
using LCShrinkRay.comp;
using System;
using System.Collections.Generic;
using System.Text;
using static LCShrinkRay.comp.Shrinking;
using System.Threading.Tasks;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem;
using UnityEngine;
using GameNetcodeStuff;
using LCShrinkRay.coroutines;
using UnityEngine.InputSystem.Utilities;

namespace LCShrinkRay.patches
{
    [HarmonyPatch]
    internal class DebugPatches
    {
        private static int waitFrames = 0;
        public static bool throwRoutineRunning = false;

        public static void unsetThrowRoutine() { throwRoutineRunning = false; }

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
                    Plugin.log("Simulating fake broadcast");
                    Network.Broadcast("OnShrinking", new ShrinkData() { playerObjName = "Player", shrinkage = 0.4f });
                }

                else if (Keyboard.current.f2Key.wasPressedThisFrame)
                {
                    Plugin.log("Shrinking player model");
                    var playerObj = Shrinking.GetPlayerObject(Shrinking.Instance.clientId);
                    Shrinking.Instance.ShrinkPlayer(playerObj, 0.4f, Shrinking.Instance.clientId);
                    Shrinking.Instance.sendShrinkMessage(playerObj, 0.4f);
                }

                else if (Keyboard.current.f3Key.wasPressedThisFrame)
                {
                    Plugin.log("Growing player model");
                    var playerObj = Shrinking.GetPlayerObject(Shrinking.Instance.clientId);
                    Shrinking.Instance.ShrinkPlayer(playerObj, 1f, Shrinking.Instance.clientId);
                    Shrinking.Instance.sendShrinkMessage(playerObj, 1f);
                }

                else if (Keyboard.current.f4Key.wasPressedThisFrame)
                {
                    for (int i = 1; i < GameNetworkManager.Instance.connectedPlayers; i++)
                    {
                        string playerName = "Player (" + i.ToString() + ")";
                        var playerObj = GameObject.Find(playerName);
                        if (playerObj != null)
                        {
                            Plugin.log("Shrinking " + playerName + " model");
                            Shrinking.Instance.ShrinkPlayer(playerObj, 0.4f, (ulong)i);
                            Shrinking.Instance.sendShrinkMessage(playerObj, 0.4f);
                        }
                    }
                }

                else if (Keyboard.current.f5Key.wasPressedThisFrame)
                {
                    for (int i = 1; i < GameNetworkManager.Instance.connectedPlayers; i++)
                    {
                        string playerName = "Player (" + i.ToString() + ")";
                        var playerObj = GameObject.Find(playerName);
                        if (playerObj != null)
                        {
                            Plugin.log("Growing " + playerName + " model");
                            Shrinking.Instance.ShrinkPlayer(playerObj, 1f, (ulong)i);
                            Shrinking.Instance.sendShrinkMessage(playerObj, 1f);
                        }
                    }
                }

                else if (Keyboard.current.f6Key.wasPressedThisFrame)
                {
                    string enemyTypes = "";
                    RoundManager.Instance.currentLevel.Enemies.ForEach(enemyType => { enemyTypes += " " + enemyType.enemyType.name; });
                    Plugin.log("EnemyTypes:" + enemyTypes); // Centipede SandSpider HoarderBug Flowerman Crawler Blob DressGirl Puffer Nutcracker

                    int enemyIndex = RoundManager.Instance.currentLevel.Enemies.FindIndex(spawnableEnemy => spawnableEnemy.enemyType.name == "HoarderBug");
                    if (enemyIndex != -1)
                    {
                        var location = __instance.transform.position + __instance.transform.forward * 3;
                        RoundManager.Instance.SpawnEnemyOnServer(location, 0f, enemyIndex);

                        /* I tried so hard and got so far, but in the end... there's still an errooooorrrrr

                        var currentLevel = RoundManager.Instance.currentLevel;
                        var enemyList = outsideEnemy ? currentLevel.OutsideEnemies : currentLevel.Enemies;
                        var enemy = Instantiate(enemyList[enemyIndex].enemyType.enemyPrefab, pos, Quaternion.Euler(Vector3.zero));
                        enemy.GetComponentInChildren<NetworkObject>().Spawn(true);*/

                    }
                }

                //waitFrames = 5;

            }
            catch (Exception e)
            {
                Plugin.log("Error in Update() [DebugKeys]: " + e.Message);
            }
        }
    }
}
