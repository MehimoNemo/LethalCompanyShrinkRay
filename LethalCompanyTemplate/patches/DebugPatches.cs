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

namespace LCShrinkRay.patches
{
    [HarmonyPatch]
    internal class DebugPatches
    {
        private static int waitFrames = 0;
        [HarmonyPatch(typeof(PlayerControllerB), "Update")]
        [HarmonyPostfix]
        public static async void OnUpdate(PlayerControllerB __instance)
        {
            if (waitFrames > 0) // Don't execute it multiple times
            {
                waitFrames--;
                return;
            }

            try
            {
                if (Keyboard.current.oKey.wasPressedThisFrame)
                {
                    Plugin.log("Simulating fake broadcast");
                    Network.Broadcast("OnShrinking", new ShrinkData() { playerObjName = "Player", shrinkage = 0.4f });
                }

                else if (Keyboard.current.nKey.wasPressedThisFrame)
                {
                    Plugin.log("Shrinking player model");
                    var playerObj = Shrinking.GetPlayerObject(Shrinking.Instance.clientId);
                    Shrinking.Instance.ShrinkPlayer(playerObj, 0.4f, Shrinking.Instance.clientId);
                    Shrinking.Instance.sendShrinkMessage(playerObj, 0.4f);
                }

                else if (Keyboard.current.mKey.wasPressedThisFrame)
                {
                    Plugin.log("Growing player model");
                    var playerObj = Shrinking.GetPlayerObject(Shrinking.Instance.clientId);
                    Shrinking.Instance.ShrinkPlayer(playerObj, 1f, Shrinking.Instance.clientId);
                    Shrinking.Instance.sendShrinkMessage(playerObj, 1f);
                }

                else if (Keyboard.current.jKey.wasPressedThisFrame)
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

                else if (Keyboard.current.kKey.wasPressedThisFrame)
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

                else if (Keyboard.current.lKey.wasPressedThisFrame)
                {
                    string enemyTypes = "";
                    RoundManager.Instance.currentLevel.Enemies.ForEach(enemyType => { enemyTypes += " " + enemyType.enemyType.name; });
                    Plugin.log("EnemyTypes:" + enemyTypes);

                    int enemyIndex = RoundManager.Instance.currentLevel.Enemies.FindIndex(spawnableEnemy => spawnableEnemy.enemyType.name == "Crawler");
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

                else if (Keyboard.current.f9Key.wasPressedThisFrame) // still getting called 4 times... whatever
                {
                    Plugin.log("AddForceAtPosition1");
                    var rb = __instance.gameObject.GetComponent<Rigidbody>();
                    rb.isKinematic = false;
                    rb.freezeRotation = true;
                    rb.AddForceAtPosition(new UnityEngine.Vector3(20, 40, 0), __instance.transform.position + __instance.transform.forward * 2f, ForceMode.Impulse);
                    await resetToKinetic(rb);
                }
                else
                    return;

                waitFrames = 5;

            }
            catch (Exception e)
            {
                Plugin.log("Error in Update() [DebugKeys]: " + e.Message);
            }
        }

        public static async Task resetToKinetic(Rigidbody rb)
        {
            Plugin.log("resetToKinetic");
            await Task.Delay(500);
            rb.isKinematic = true;
            rb.freezeRotation = false;
        }
    }
}
