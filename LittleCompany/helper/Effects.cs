using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace LittleCompany.helper
{
    [HarmonyPatch]
    internal class Effects
    {
        private static GameObject _circleHighlightPrefab = null;
        public static GameObject CircleHighlight
        {
            get
            {
                if (_circleHighlightPrefab == null)
                {
                    _circleHighlightPrefab = AssetLoader.littleCompanyAsset?.LoadAsset<GameObject>(Path.Combine(AssetLoader.BaseAssetPath, "Shrink/HighlightingCircle.prefab"));
                    if (_circleHighlightPrefab == null) return null;
                }

                var ch = UnityEngine.Object.Instantiate(_circleHighlightPrefab);
                return ch;
            }
        }

        private static GameObject _deathPoof = null;
        public static GameObject DeathPoof
        {
            get
            {
                if (_deathPoof == null)
                {
                    _deathPoof = AssetLoader.littleCompanyAsset?.LoadAsset<GameObject>(Path.Combine(AssetLoader.BaseAssetPath, "grabbable/Poof.prefab"));
                    if (_deathPoof == null) return null;
                }

                var ch = UnityEngine.Object.Instantiate(_deathPoof);
                return ch;
            }
        }

        public static void LightningStrikeAtPosition(Vector3 position) => GameNetworkManager.Instance.StartCoroutine(LightningStrikeAtPositionCoroutine(position));

        private static IEnumerator LightningStrikeAtPositionCoroutine(Vector3 position)
        {
            var stormyWeather = UnityEngine.Object.FindObjectOfType<StormyWeather>(includeInactive: true);
            if (stormyWeather == null)
            {
                Plugin.Log("Unable to create lightning at position " + position, Plugin.LogType.Warning);
                yield break;
            }

            bool currentWeatherIsStormy = true;
            if (!stormyWeather.gameObject.activeSelf)
            {
                currentWeatherIsStormy = false;
                stormyWeather.gameObject.SetActive(true);
            }

            stormyWeather.LightningStrike(position, true);

            if (currentWeatherIsStormy)
                yield break;

            if (stormyWeather.targetedStrikeAudio != null)
                yield return new WaitWhile(() => stormyWeather.targetedStrikeAudio.isPlaying);
            else
                yield return new WaitForSeconds(2.5f);

            stormyWeather.gameObject.SetActive(false);
        }

        #region Fixes
        [HarmonyPatch(typeof(StormyWeather), nameof(StormyWeather.LightningStrike))]
        [HarmonyPrefix]
        public static void TargetedThunderFix(ref System.Random ___targetedThunderRandom, ref System.Random ___seed)
        {
            // Fixes LightningStrike throwing exception when weather isn't stormy
            if (___targetedThunderRandom == null)
                ___targetedThunderRandom = new System.Random(StartOfRound.Instance.randomMapSeed);

            if (___seed == null)
                ___seed = new System.Random(StartOfRound.Instance.randomMapSeed);
        }
        #endregion
    }
}
