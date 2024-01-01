using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using GameNetcodeStuff;
using BepInEx.Configuration;
using DunGen;
using UnityEngine.InputSystem;
using static UnityEngine.ParticleSystem.PlaybackState;
using LCShrinkRay.comp;
using LC_API.ServerAPI;
using LC_API;
using LCShrinkRay.patches;
using LCShrinkRay.Config;
using System;

namespace LCShrinkRay
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private readonly Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);
        private static Plugin Instance;
        internal ManualLogSource mls;
        private GameObject playerObject;

        public static ConfigFile bepInExConfig() { return Instance.Config; }

        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            if (Instance == null)
                Instance = this;

            mls = BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_GUID);

            try
            {
                ModConfig.Instance.load();
            }
            catch(Exception ex)
            {
                mls.LogError(ex.Message);
            }

            mls.LogInfo("The Test Mod Has Awoken");

            harmony.PatchAll(typeof(Plugin));
            //harmony.PatchAll(typeof(SoundManagerPatch));
            harmony.PatchAll(typeof(GameNetworkManagerPatch));
            //harmony.PatchAll(typeof(PlayerControllerBPatch));
            //Networking.GetString += Shrinking.ShGetString;
        }


        
        private void OnDestroy()
        {
            GameObject gameObject = new GameObject("SHRINKING");
            DontDestroyOnLoad(gameObject);
            gameObject.AddComponent<Shrinking>();
            Logger.LogInfo($"SHRINKING Started!");
        }
    }
}