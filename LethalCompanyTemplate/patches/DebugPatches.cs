using HarmonyLib;
using System;
using UnityEngine.InputSystem;
using GameNetcodeStuff;
using LCShrinkRay.helper;
using static LCShrinkRay.comp.ShrinkRay;
using LCShrinkRay.comp;
using UnityEngine;
using static LCShrinkRay.helper.Moons;

namespace LCShrinkRay.patches
{
    [HarmonyPatch]
    internal class DebugPatches
    {
        private static int waitFrames = 0;
        public static bool throwRoutineRunning = false;

        public static void UnsetThrowRoutine() { throwRoutineRunning = false; }

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
                    Plugin.Log("EnemyTypes:" + enemyTypes); // Centipede SandSpider HoarderBug Flowerman Crawler Blob DressGirl Puffer Nutcracker

                    int enemyIndex = RoundManager.Instance.currentLevel.Enemies.FindIndex(spawnableEnemy => spawnableEnemy.enemyType.name == "Centipede");
                    if (enemyIndex != -1)
                    {
                        var location = __instance.transform.position + __instance.transform.forward * 3;
                        RoundManager.Instance.SpawnEnemyOnServer(location, 0f, enemyIndex);

                        // I tried so hard and got so far, but in the end... there's still an errooooorrrrr
                    }
                }

                else if (Keyboard.current.f2Key.wasPressedThisFrame)
                {
                    Plugin.Log("Shrinking player model");
                    ShrinkRay.debugOnPlayerModificationWorkaround(PlayerInfo.CurrentPlayer, ModificationType.Shrinking);
                }

                else if (Keyboard.current.f3Key.wasPressedThisFrame)
                {
                    Plugin.Log("Growing player model");
                    ShrinkRay.debugOnPlayerModificationWorkaround(PlayerInfo.CurrentPlayer, ModificationType.Enlarging);
                }

                // /spawnenemy Hoarding bug a=1 p=@me
                else if (Keyboard.current.f5Key.wasPressedThisFrame)
                {
                    if (HoarderBugAI.grabbableObjectsInMap == null)
                    {
                        Plugin.Log("No grabbable hoarder bug objects.");
                        return;
                    }

                    var output = "Grabbable hoarder bug objects:\n";
                    output += "------------------------------\n";
                    foreach (var item in HoarderBugAI.grabbableObjectsInMap)
                        output += item.name + "\n";
                    output += "------------------------------\n";
                    Plugin.Log(output);
                }

                else if (Keyboard.current.f6Key.wasPressedThisFrame)
                {
                    if (HoarderBugAI.HoarderBugItems == null)
                    {
                        Plugin.Log("No hoarder bug items.");
                        return;
                    }

                    var output = "Grabbable hoarder bug items:\n";
                    output += "------------------------------\n";
                    foreach (var item in HoarderBugAI.HoarderBugItems)
                        output += item.itemGrabbableObject.name + ": " + item.status.ToString() + "\n";
                    output += "------------------------------\n";
                    Plugin.Log(output);
                }

                else if (Keyboard.current.f7Key.wasPressedThisFrame)
                {
                    if (HoarderBugAIPatch.latestNestPosition != Vector3.zero)
                    {
                        Plugin.Log("Teleporting to latest hoarder bug nest position.");
                        PlayerInfo.CurrentPlayer.TeleportPlayer(HoarderBugAIPatch.latestNestPosition);
                    }
                    else
                        Plugin.Log("No hoarder bug nest yet..");
                }

                else if (Keyboard.current.f8Key.wasPressedThisFrame)
                {
                    var gpoList = UnityEngine.Object.FindObjectsOfType<GrabbablePlayerObject>();
                    foreach (var gpo in gpoList)
                    {
                        if (gpo.grabbableToEnemies)
                        {
                            Plugin.Log("Added as stolen hoarding bug item");
                            HoarderBugAI.HoarderBugItems.Add(new HoarderBugItem(gpo, HoarderBugItemStatus.Stolen, gpo.transform.position));
                        }
                    }
                }


                else if (Keyboard.current.f9Key.wasPressedThisFrame)
                {
                    // Print position to log
                    Plugin.Log("Current position: " + PlayerInfo.CurrentPlayer.gameObject.transform.position + ". Moon: " + RoundManager.Instance.currentLevel.name);
                }

                else if (Keyboard.current.f10Key.wasPressedThisFrame)
                {
                    if (StartOfRound.Instance.inShipPhase || !StartOfRound.Instance.shipHasLanded) return;

                    // Teleport inside ship
                    PlayerInfo.CurrentPlayer.gameObject.transform.position = new Vector3(2.84f, 0.29f, -14.41f);
                }

                else if (Keyboard.current.f11Key.wasPressedThisFrame)
                {
                    if (StartOfRound.Instance.inShipPhase) return;

                    // Teleport outside
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

                    PlayerInfo.CurrentPlayer.gameObject.transform.position = pos;
                }
                
                else if (Keyboard.current.f12Key.wasPressedThisFrame)
                {
                    if (StartOfRound.Instance.inShipPhase) return;

                    // Teleport inside
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

                    PlayerInfo.CurrentPlayer.gameObject.transform.position = pos;
                }
                else
                    return;

                waitFrames = 5;

            }
            catch (Exception e)
            {
                Plugin.Log("Error in Update() [DebugKeys]: " + e.Message);
            }
        }

        public static GameObject CreateCube(Transform parent, Color color)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //cube.transform.SetParent(parent);
            if (cube.TryGetComponent(out BoxCollider boxCollider))
                boxCollider.enabled = false;

            if (cube.TryGetComponent(out MeshRenderer meshRenderer))
            {
                Plugin.Log("Has mesh renderer");
                meshRenderer.sharedMaterial = new Material(Shader.Find("HDRP/Lit"));
                meshRenderer.sharedMaterial.color = color;
                meshRenderer.enabled = true;
            }

            return cube;
        }
    }
}
