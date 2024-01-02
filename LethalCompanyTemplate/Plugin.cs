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
using LC_API.Networking;

namespace LCShrinkRay
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private readonly Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);
        private static Plugin Instance;
        private static ManualLogSource mls;
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

            mls.LogInfo(PluginInfo.PLUGIN_NAME + " mod has awoken!");

            harmony.PatchAll(typeof(Plugin));
            //harmony.PatchAll(typeof(SoundManagerPatch));
            harmony.PatchAll(typeof(GameNetworkManagerPatch));
            //harmony.PatchAll(typeof(PlayerControllerBPatch));
            harmony.PatchAll(typeof(ModConfig.SyncHandshake));
            //Networking.GetString += Shrinking.ShGetString;

            GameObject gameObject = new GameObject("SHRINKING");
            DontDestroyOnLoad(gameObject);
            gameObject.AddComponent<Shrinking>();
            Logger.LogInfo($"SHRINKING Started!");

            try
            {
                Network.RegisterAll(typeof(Shrinking)); // LC_API Network Setup
            }
            catch (Exception e)
            {
                mls.LogError(e.Message);
            }
        }

        public enum LogType
        {
            Message,
            Warning,
            Error,
            Fatal
        }

        public static void log(string message, LogType type = LogType.Message)
        {
            switch(type)
            {
                case LogType.Message: mls.LogMessage(message); break;
                case LogType.Warning: mls.LogWarning(message); break;
                case LogType.Error: mls.LogError(message); break;
                case LogType.Fatal: mls.LogFatal(message); break;
            }
        }

        
        private void OnDestroy()
        {
            /*GameObject gameObject = new GameObject("SHRINKING");
            DontDestroyOnLoad(gameObject);
            gameObject.AddComponent<Shrinking>();
            Logger.LogInfo($"SHRINKING Started!");*/
        }
    }
}