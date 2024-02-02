using HarmonyLib;
using LCShrinkRay.comp;
using LCShrinkRay.Config;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Windows;

namespace LCShrinkRay.patches
{
    [HarmonyPatch]
    class ThumperAIPatch
    {
        [HarmonyPatch(typeof(CrawlerAI), "OnCollideWithPlayer")]
        [HarmonyPostfix]
        public static void OnCollideWithPlayer(CrawlerAI __instance, Collider other)
        {
            var playerControllerB = __instance.MeetsStandardPlayerCollisionConditions(other);
            if (!playerControllerB)
                return;

            switch (ModConfig.Instance.values.thumperBehaviour)
            {
                case ModConfig.ThumperBehaviour.OneShot:
                    playerControllerB.KillPlayer(bodyVelocity: default(Vector3), spawnBody: false, CauseOfDeath.Mauling);
                    break;
                /*case ThumperBehaviour.Bumper: // NOT WORKING YET
                    Plugin.log("Forward: " + __instance.transform.forward.ToString());
                    var force = __instance.transform.forward * 10000f; // maybe forward is 0?
                    force.x = 4.2f;
                    Plugin.log("Forward after adjustments: " + force);

                    var rb = playerControllerB.gameObject.GetComponent<Rigidbody>();
                    if (!rb)
                    {
                        Plugin.log("Adding rigidbody");
                        rb = playerControllerB.gameObject.AddComponent<Rigidbody>();
                        if (!rb)
                        {
                            Plugin.log("No rigidbody", Plugin.LogType.Error);
                            return;
                        }
                    }
                    rb.isKinematic = false;
                    rb.freezeRotation = true;
                    rb.AddForce(force, ForceMode.Impulse);
                    resetToKinetic(rb).GetAwaiter();
                    break;*/
                default:
                    break;
            }
        }
        public static async Task resetToKinetic(Rigidbody rb)
        {
            await Task.Delay(500);
            rb.isKinematic = true;
            rb.freezeRotation = false;
        }
    }
}
