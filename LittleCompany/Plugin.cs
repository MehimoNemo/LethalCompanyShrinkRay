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
using LittleCompany.events.item;
using UnityEngine.Audio;
using LittleCompany.patches.enemy_behaviours;

namespace LittleCompany
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency(SCP956CompatibilityPatch.SCP956ApiReferenceChain, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(MoreCompanyAudioCompatibilityPatch.MoreCompanyReferenceChain, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(LethalEmotesApiCompatibility.LethalEmotesApiReferenceChain, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(ScrapManagementFacade.LethalLevelLoaderReferenceChain, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(ScrapManagementFacade.LethalLibReferenceChain, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(LethalVRMCompatibilityComponent.LethalVRMApiReferenceChain, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(LethalVRMCompatibilityComponent.BetterLethalVRMApiReferenceChain, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(ModelReplacementApiCompatibilityComponent.ModelReplacementApiReferenceChain, BepInDependency.DependencyFlags.SoftDependency)]
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
            harmony.PatchAll(typeof(ModConfig.SyncHandshake));
            harmony.PatchAll(typeof(GameNetworkManagerPatch));
            harmony.PatchAll(typeof(PlayerCountChangeDetection));
            harmony.PatchAll(typeof(PlayerMultiplierPatch));
            harmony.PatchAll(typeof(DeskPatch));
            harmony.PatchAll(typeof(QuicksandPatch));
            harmony.PatchAll(typeof(TurretPatch));
            harmony.PatchAll(typeof(AudioPatches));
            harmony.PatchAll(typeof(VehicleControllerPatch));
            harmony.PatchAll(typeof(InteractTriggerPatch));

            // components
            harmony.PatchAll(typeof(Vents));
            harmony.PatchAll(typeof(ShrinkRay));
            harmony.PatchAll(typeof(GrabbablePlayerList));

            // helper
            harmony.PatchAll(typeof(Effects));

            // enemy
            harmony.PatchAll(typeof(ThumperAIPatch));
            harmony.PatchAll(typeof(HoarderBugAIPatch));
            harmony.PatchAll(typeof(CentipedeAIPatch));
            harmony.PatchAll(typeof(ForestGiantAIPatch));

            // enemy events
            harmony.PatchAll(typeof(BrackenEventHandler));
            harmony.PatchAll(typeof(RobotEventHandler));
            harmony.PatchAll(typeof(ThumperEventHandler));

            // items
            harmony.PatchAll(typeof(ItemSavingPatch));
            harmony.PatchAll(typeof(ScreenBlockingGrabbablePatch));

            // item events
            harmony.PatchAll(typeof(FlashlightEventHandler));
            harmony.PatchAll(typeof(GiftBoxEventHandler));
            harmony.PatchAll(typeof(ShovelEventHandler));
            harmony.PatchAll(typeof(KeyEventHandler));
            harmony.PatchAll(typeof(SprayPaintEventHandler));
            harmony.PatchAll(typeof(ShotgunEventHandler));

            // ship objects
            harmony.PatchAll(typeof(ShipBuildModeManagerPatch));
            harmony.PatchAll(typeof(TerminalPatch));

            // compatibility
            harmony.PatchAll(typeof(ModdedDungeonEntrancePatch));
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
            if (MoreCompanyAudioCompatibilityPatch.compatEnabled) {
                Log("enabling MoreCompanyAudioCompatibility");
                harmony.Unpatch(typeof(AudioMixer).GetMethod("SetFloat"), HarmonyPatchType.Prefix, MoreCompanyAudioCompatibilityPatch.MoreCompanyReferenceChain);
                harmony.PatchAll(typeof(MoreCompanyAudioCompatibilityPatch));
            }
            if (SCP956CompatibilityPatch.compatEnabled)
            {
                Log("enabling SCP956Compatibility");
                harmony.PatchAll(typeof(SCP956CompatibilityPatch));
            }
            if (ModelXCosmeticsComponent.compatEnabled)
            {
                Log("enabling Compatibility for MoreCompany cosmetics on ModelReplacementApi model");
                harmony.PatchAll(typeof(ModelXCosmeticsPatch));
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