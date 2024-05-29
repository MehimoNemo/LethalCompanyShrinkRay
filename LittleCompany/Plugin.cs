using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using BepInEx.Configuration;
using LittleCompany.patches;
using LittleCompany.patches.EnemyBehaviours;
using LittleCompany.Config;
using System.Reflection;
using LittleCompany.components;
using LittleCompany.compatibility;
using LittleCompany.events.enemy;
using LittleCompany.helper;
using LittleCompany.dependency;

namespace LittleCompany
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("me.swipez.melonloader.morecompany", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(LethalEmotesApiCompatibility.LethalEmotesApiReferenceChain, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(ScrapManagementFacade.LethalLevelLoaderReferenceChain, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(ScrapManagementFacade.LethalLibReferenceChain, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(LethalVRMCompatibilityComponent.LethalVRMApiReferenceChain, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(LethalVRMCompatibilityComponent.BetterLethalVRMApiReferenceChain, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(LCOfficeCompatibility.LCOfficeReferenceChain, BepInDependency.DependencyFlags.SoftDependency)]
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
            harmony.PatchAll(typeof(PlayerMultiplierPatch));
            harmony.PatchAll(typeof(ModConfig.SyncHandshake));
            harmony.PatchAll(typeof(ThumperAIPatch));
            harmony.PatchAll(typeof(HoarderBugAIPatch));
            harmony.PatchAll(typeof(PlayerCountChangeDetection));
            harmony.PatchAll(typeof(DeskPatch));
            harmony.PatchAll(typeof(ScreenBlockingGrabbablePatch));
            harmony.PatchAll(typeof(CentipedeAIPatch));
            harmony.PatchAll(typeof(ModdedDungeonEntrancePatch));
            harmony.PatchAll(typeof(TerminalPatch));
            harmony.PatchAll(typeof(QuicksandPatch));
            harmony.PatchAll(typeof(TurretPatch));
            harmony.PatchAll(typeof(AudioPatches));
            harmony.PatchAll(typeof(GiftBoxItemPatch));

            // components
            harmony.PatchAll(typeof(Vents));
            harmony.PatchAll(typeof(GrabbablePlayerList));

            // helper
            harmony.PatchAll(typeof(Effects));

            // events
            harmony.PatchAll(typeof(BrackenEventHandler));
            harmony.PatchAll(typeof(RobotEventHandler));
            harmony.PatchAll(typeof(ThumperEventHandler));

            // Compatibility
            if (LethalEmotesApiCompatibility.compatEnabled)
            {
                Log("enabling LethalEmotesApiCompatibility");
                harmony.PatchAll(typeof(LethalEmotesApiCompatibility));
            }
            if (ModelReplacementApiCompatibilityComponent.compatEnabled)
            {
                Log("enabling ModelReplacementApiCompatibility");
                harmony.PatchAll(typeof(ModelReplacementApiCompatibilityPatch));
            }
            if (LethalVRMCompatibilityComponent.compatEnabled)
            {
                Log("enabling LethalVRMCompatibility");
                harmony.PatchAll(typeof(LethalVRMCompatibilityPatch));
            }
            if (LCOfficeCompatibility.compatEnabled)
            {
                Log("enabling LCOfficeCompatibility");
                harmony.PatchAll(typeof(LCOfficeCompatibility));
            }

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