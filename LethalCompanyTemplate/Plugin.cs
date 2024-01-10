using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using BepInEx.Configuration;
using LCShrinkRay.comp;
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
                ModConfig.Instance.setup();
                Shrinking.Instance.setup(); // todo: split shrinking.cs further into smaller classes, so that this line can get removed
            }
            catch(Exception ex)
            {
                mls.LogError(ex.Message);
            }

            mls.LogInfo(PluginInfo.PLUGIN_NAME + " mod has awoken!");

            harmony.PatchAll(typeof(Plugin));
            //harmony.PatchAll(typeof(SoundManagerPatch));
            harmony.PatchAll(typeof(GameNetworkManagerPatch));
            harmony.PatchAll(typeof(PlayerControllerBPatch));
            harmony.PatchAll(typeof(ModConfig.SyncHandshake));
            harmony.PatchAll(typeof(ThumperAIPatch));
            harmony.PatchAll(typeof(HoarderBugAIPatch));
            harmony.PatchAll(typeof(PlayerCountChangeDetection));

            // Debug
            ModConfig.debugMode = true;
            if(ModConfig.debugMode)
                harmony.PatchAll(typeof(DebugPatches));

            try
            {
                // LC_API Network Setup
                Network.RegisterAll(typeof(Shrinking));
                Network.RegisterAll(typeof(GrabbablePlayerObject));
                Network.RegisterAll(typeof(GrabbablePlayerList));
            }
            catch (Exception e)
            {
                mls.LogError(e.Message);
            }

            ShrinkRay.AddToGame();
        }

        public enum LogType
        {
            Message,
            Warning,
            Error,
            Fatal,
            Debug
        }

        internal static void log(string message, LogType type = LogType.Debug)
        {
            if (type == LogType.Debug && !ModConfig.debugMode)
                return;

            switch(type)
            {
                case LogType.Warning: mls.LogWarning(message); break;
                case LogType.Error: mls.LogError(message); break;
                case LogType.Fatal: mls.LogFatal(message); break;
                default: mls.LogMessage(message); break;
            }
        }
    }
}