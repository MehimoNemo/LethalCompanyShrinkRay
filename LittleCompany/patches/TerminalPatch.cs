using GameNetcodeStuff;
using HarmonyLib;
using LittleCompany.helper;
using LittleCompany.modifications;
using System.Collections;
using UnityEngine;

namespace LittleCompany.patches
{
    [HarmonyPatch]
    internal class TerminalPatch
    {
        private static float _realPlayerScale = 1f;
        private static Coroutine _scalingCoroutine;

        [HarmonyPatch(typeof(Terminal), "BeginUsingTerminal")]
        [HarmonyPrefix]
        public static void BeginUsingTerminal(Terminal __instance)
        {
            _realPlayerScale = PlayerInfo.CurrentPlayerScale;
            _scalingCoroutine = GameNetworkManager.Instance.StartCoroutine(ScaleToTerminalSize(__instance));
        }

        public static IEnumerator ScaleToTerminalSize(Terminal terminal)
        {
            float time = 0f, duration = 0.5f;
            while (time < duration)
            {
                var scale = Mathf.Lerp(_realPlayerScale, ShipObjectModification.ScalingOf(terminal.placeableObject).RelativeScale, time / duration);
                PlayerInfo.CurrentPlayer.transform.localScale = Vector3.one * scale;

                time += Time.deltaTime;
                yield return null;
            }
        }

        [HarmonyPatch(typeof(Terminal), "QuitTerminal")]
        [HarmonyPostfix]
        public static void QuitTerminal(ref InteractTrigger ___terminalTrigger)
        {
            if (_scalingCoroutine != null)
                GameNetworkManager.Instance.StopCoroutine(_scalingCoroutine);

            PlayerInfo.CurrentPlayer.transform.localScale = Vector3.one * _realPlayerScale;
        }
    }
}
