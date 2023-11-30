using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using GameNetcodeStuff;
using HarmonyLib;
using LCShrinkRay.comp;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Yoga;

namespace LCShrinkRay.patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class PlayerControllerBPatch
    {
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void voiceShrinker(ref float ___num11)
        {
            ulong clientId = GameNetworkManager.Instance.localPlayerController.playerClientId;
            if (clientId != 239)
            {
                string myPlayerObjectName = "Player";
                if (clientId != 0)
                {
                    myPlayerObjectName = "Player (" + clientId.ToString() + ")";
                }
                GameObject myPlayerObject = GameObject.Find(myPlayerObjectName);
                float myScale = myPlayerObject.transform.localScale.x;
                ___num11 = -0.417f * myScale + 0.417f;
            }
        }
    }
}


