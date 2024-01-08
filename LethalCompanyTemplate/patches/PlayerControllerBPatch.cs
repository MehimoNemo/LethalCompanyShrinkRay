using GameNetcodeStuff;
using HarmonyLib;
using LC_API.Networking;
using LCShrinkRay.comp;
using UnityEngine.InputSystem;
using UnityEngine;

namespace LCShrinkRay.patches
{
    [HarmonyPatch]
    internal class PlayerControllerBPatch
    {
        //static bool logShowed = false, log2Showed = false;
        [HarmonyPatch(typeof(PlayerControllerB), "Update")]
        [HarmonyPostfix]
        public static void OnUpdate(PlayerControllerB __instance)
        {
            //SoundManager.Instance.playerVoicePitchTargets[__instance.playerClientId] = 1.2f;
            Shrinking.Instance.Update();

            if (__instance.currentlyHeldObject != null && __instance.currentlyHeldObject.GetType() == typeof(GrabbablePlayerObject))
            {
                /*if(!logShowed)
                {
                    Plugin.log("PlayerControllerB.Update on grabbing Player...");
                    Plugin.log("player: " + (__instance).ToString());
                    Plugin.log("playerHeldBy.currentlyHeldObject: " + __instance.currentlyHeldObject);
                    logShowed = true;
                }
                
                var grabbedPlayer = __instance.currentlyHeldObject as GrabbablePlayerObject;
                if(grabbedPlayer != null)
                {
                    if (!log2Showed)
                    {
                        Plugin.log("PlayerControllerB.Update on grabbing Player (Pt 2)...");
                        Plugin.log("grabbedPlayer: " + (grabbedPlayer).ToString());
                        Plugin.log("playerHeldBy: " + (grabbedPlayer.playerHeldBy).ToString());
                        Plugin.log("isHeld: " + grabbedPlayer.isHeld.ToString());
                        log2Showed = true;
                    }
                    //this looks like trash unfortunately
                    grabbedPlayer.transform.position = __instance.transform.position;
                    //change this
                    Vector3 targetPosition = __instance.localItemHolder.transform.position;
                    Vector3 targetUp = -(grabbedPlayer.transform.position - targetPosition).normalized;
                    Quaternion targetRotation = Quaternion.FromToRotation(__instance.transform.up, targetUp) * grabbedPlayer.transform.rotation;
                    //Quaternion targetRotation = Quaternion.FromToRotation(grabbedPlayer.transform.up, targetUp);
                    grabbedPlayer.transform.rotation = Quaternion.Slerp(grabbedPlayer.transform.rotation, targetRotation, 50 * Time.deltaTime);
                    if (grabbedPlayer.playerHeldBy != null)
                        grabbedPlayer.playerHeldBy.playerCollider.enabled = false;
                    else
                        Plugin.log("playerHeldBy is null.. this ain't normal..", Plugin.LogType.Warning);
                }
                */
            }
        }

        [NetworkMessage("DemandDropFromPlayer")]
        public static void DemandDropFromPlayer(ulong sender, string playerID)
        {
            Plugin.log("A player demands to be dropped from player " + playerID);
            if (StartOfRound.Instance.localPlayerController.playerClientId == ulong.Parse(playerID)) // I have to drop him...
            {
                Plugin.log("I have to drop them... sadly!", Plugin.LogType.Warning);
                StartOfRound.Instance.localPlayerController.DiscardHeldObject();
            }
        }
    }

    // todo: itemActive maybe here?
}




