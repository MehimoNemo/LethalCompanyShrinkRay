using HarmonyLib;
using System;
using UnityEngine.InputSystem;
using GameNetcodeStuff;
using LCShrinkRay.helper;
using static LCShrinkRay.comp.ShrinkRay;
using LCShrinkRay.comp;

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
                    string enemyTypes = "";
                    RoundManager.Instance.currentLevel.Enemies.ForEach(enemyType => { enemyTypes += " " + enemyType.enemyType.name; });
                    Plugin.log("EnemyTypes:" + enemyTypes); // Centipede SandSpider HoarderBug Flowerman Crawler Blob DressGirl Puffer Nutcracker

                    int enemyIndex = RoundManager.Instance.currentLevel.Enemies.FindIndex(spawnableEnemy => spawnableEnemy.enemyType.name == "HoarderBug");
                    if (enemyIndex != -1)
                    {
                        var location = __instance.transform.position + __instance.transform.forward * 3;
                        RoundManager.Instance.SpawnEnemyOnServer(location, 0f, enemyIndex);

                        // I tried so hard and got so far, but in the end... there's still an errooooorrrrr
                    }
                }
                
                else if (Keyboard.current.f2Key.wasPressedThisFrame)
                {
                    Plugin.log("Shrinking player model");
                    ShrinkRay.debugOnPlayerModificationWorkaround(PlayerInfo.CurrentPlayer, ModificationType.Shrinking);
                }

                else if (Keyboard.current.f3Key.wasPressedThisFrame)
                {
                    Plugin.log("Growing player model");
                    ShrinkRay.debugOnPlayerModificationWorkaround(PlayerInfo.CurrentPlayer, ModificationType.Enlarging);
                }

                else if (Keyboard.current.f4Key.wasPressedThisFrame)
                {
                    foreach(var pcb in StartOfRound.Instance.allPlayerScripts)
                    {
                        Plugin.log("Shrinking Player (" + pcb.playerClientId + ")");
                        ShrinkRay.debugOnPlayerModificationWorkaround(pcb, ModificationType.Shrinking);
                    }
                }

                else if (Keyboard.current.f4Key.wasPressedThisFrame)
                {
                    foreach (var pcb in StartOfRound.Instance.allPlayerScripts)
                    {
                        Plugin.log("Growing Player (" + pcb.playerClientId + ")");
                        ShrinkRay.debugOnPlayerModificationWorkaround(pcb, ModificationType.Enlarging);
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
