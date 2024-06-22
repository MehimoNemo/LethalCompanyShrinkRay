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
        private static float _realTerminalScale = 1f;
        private static Coroutine _scalingCoroutine;

        [HarmonyPatch(typeof(Terminal), "BeginUsingTerminal")]
        [HarmonyPrefix]
        public static void BeginUsingTerminal(Terminal __instance)
        {
            _realPlayerScale = PlayerInfo.CurrentPlayerScale;
            _realTerminalScale = ShipObjectModification.ScalingOf(__instance.placeableObject).RelativeScale;
            _scalingCoroutine = __instance.StartCoroutine(ScaleToTerminalSize(__instance));
        }

        public static IEnumerator ScaleToTerminalSize(Terminal terminal)
        {
            var terminalScaling = ShipObjectModification.ScalingOf(terminal.placeableObject);
            var playerScaling = PlayerModification.ScalingOf(PlayerInfo.CurrentPlayer);
            if(terminalScaling == null || playerScaling == null)
            {
                Plugin.Log("Unable to adjust player size to terminal size.", Plugin.LogType.Error);
                yield break;
            }

            float time = 0f, duration = 0.5f;
            while (time < duration)
            {
                var playerScale = Mathf.Lerp(_realPlayerScale, 1f, time / duration);
                playerScaling.TransformToScale.localScale = playerScaling.OriginalScale * playerScale;

                var terminalScale = Mathf.Lerp(_realTerminalScale, 1f, time / duration);
                terminalScaling.TransformToScale.localScale = terminalScaling.OriginalScale * terminalScale;

                time += Time.deltaTime;
                yield return null;
            }
        }

        [HarmonyPatch(typeof(Terminal), "QuitTerminal")]
        [HarmonyPostfix]
        public static void QuitTerminal(Terminal __instance)
        {
            if (_scalingCoroutine != null)
                __instance.StopCoroutine(_scalingCoroutine);

            var terminalScaling = ShipObjectModification.ScalingOf(__instance.placeableObject);
            if (terminalScaling == null)
                Plugin.Log("Unable to reset terminal size.", Plugin.LogType.Error);
            else
                terminalScaling.TransformToScale.localScale = terminalScaling.OriginalScale * _realTerminalScale;

            var playerScaling = PlayerModification.ScalingOf(PlayerInfo.CurrentPlayer);
            if (playerScaling == null)
                Plugin.Log("Unable to reset player size after using terminal.", Plugin.LogType.Error);
            else
                playerScaling.TransformToScale.localScale = playerScaling.OriginalScale * _realPlayerScale;
        }
    }
}
