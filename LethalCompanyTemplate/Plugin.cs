using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using BepInEx.Configuration;
using LCShrinkRay.patches;
using LCShrinkRay.Config;
using System.Reflection;
using LCShrinkRay.comp;
using LCShrinkRay.compatibility;

namespace LCShrinkRay
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        #region Properties
        private readonly Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);
        private static Plugin Instance;
        private static ManualLogSource mls;

        public static ConfigFile BepInExConfig() { return Instance.Config; }
        #endregion

        private void Awake()
        {
            // Plugin startup logic
            if (Instance == null)
                Instance = this;

            mls = BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_GUID);
            ModConfig.Instance.Setup();

            NetcodePatching();
            ApplyHarmonyPatches();

            Log(PluginInfo.PLUGIN_NAME + " mod has awoken!", LogType.Message);
        }

        #region Patching
        private void ApplyHarmonyPatches()
        {
            harmony.PatchAll(typeof(Plugin));

            // patches
            harmony.PatchAll(typeof(GameNetworkManagerPatch));
            harmony.PatchAll(typeof(PlayerModificationPatch));
            harmony.PatchAll(typeof(ModConfig.SyncHandshake));
            harmony.PatchAll(typeof(ThumperAIPatch));
            harmony.PatchAll(typeof(HoarderBugAIPatch));
            harmony.PatchAll(typeof(PlayerCountChangeDetection));
            harmony.PatchAll(typeof(DeskPatch));
            harmony.PatchAll(typeof(ScreenBlockingGrabbablePatch));
            harmony.PatchAll(typeof(CentipedeAIPatch));
            harmony.PatchAll(typeof(ModdedDungeonEntrancePatch));
            harmony.PatchAll(typeof(TerminalPatch));

            // comp
            harmony.PatchAll(typeof(Vents));
            harmony.PatchAll(typeof(GrabbablePlayerList));

#if DEBUG
            harmony.PatchAll(typeof(DebugPatches));
#endif
        }

        private void NetcodePatching()
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
        }
        #endregion

        #region Logging
        public enum LogType
        {
            Message,
            Warning,
            Error,
            Fatal,
            Debug
        }

        internal static void Log(string message, LogType type = LogType.Debug)
        {
#if !DEBUG
            if (type == LogType.Debug && !ModConfig.DebugLog)
                return;
#endif

            switch(type)
            {
                case LogType.Warning: mls.LogWarning(message); break;
                case LogType.Error: mls.LogError(message); break;
                case LogType.Fatal: mls.LogFatal(message); break;
                default: mls.LogMessage(message); break;
            }
        }
#endregion
    }
}