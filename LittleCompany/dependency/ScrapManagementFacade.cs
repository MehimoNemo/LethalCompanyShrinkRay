using LethalLevelLoader;
using LethalLib.Modules;
using LittleCompany.helper;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LittleCompany.dependency
{
    public static class ScrapManagementFacade
    {
        public const string LethalLevelLoaderReferenceChain = "imabatby.lethallevelloader";
        public const string LethalLibReferenceChain = "evaisa.lethallib";

        private static IScrapManagement _ScrapManager;

        private static IScrapManagement ScrapManager
        {
            get
            {
                if (_ScrapManager == null)
                {
                    if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(LethalLevelLoaderReferenceChain))
                    {
                        Plugin.Log("Using LethalLevelLoader for scrap loading");
                        _ScrapManager = new LethalLevelLoaderScrapManagement();
                    }
                    else if(BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(LethalLibReferenceChain))
                    {
                        Plugin.Log("Using LethalLib for scrap loading");
                        _ScrapManager = new LethalLibScrapManagement();
                    }
                    else
                    {
                        Plugin.Log("Error: This mod requires either LethalLevelLoader or LethalLib to load scraps, none were detected");
                    }
                }
                return _ScrapManager;
            }
        }

        public static GameObject CloneNetworkPrefab(GameObject prefabToClone, string newName = null)
        {
            return ScrapManager.CloneNetworkPrefab(prefabToClone, newName);
        }

        public static void FixMixerGroups(GameObject prefab)
        {
            ScrapManager.FixMixerGroups(prefab);
        }

        public static void RegisterItem(Item item, bool isScrap = false, bool isBuyableItem = false, int rarity = -1, string terminalDescription = null)
        {
            ScrapManager.RegisterItem(item, isScrap, isBuyableItem, rarity, terminalDescription);
        }

        public static void RemoveScrapFromLevels(Item scrapItem, LevelTypes levelFlags = LevelTypes.None, string[] levelOverrides = null)
        {
            ScrapManager.RemoveScrapFromLevels(scrapItem, levelFlags, levelOverrides);
        }

        public static void RemoveShopItem(Item shopItem)
        {
            ScrapManager.RemoveShopItem(shopItem);
        }

        public enum LevelTypes
        {
            None = 1,
            ExperimentationLevel = 4,
            AssuranceLevel = 8,
            VowLevel = 0x10,
            OffenseLevel = 0x20,
            MarchLevel = 0x40,
            RendLevel = 0x80,
            DineLevel = 0x100,
            TitanLevel = 0x200,
            Vanilla = 0x3FC,
            Modded = 0x400,
            All = -1
        }

        internal interface IScrapManagement
        {
            public GameObject CloneNetworkPrefab(GameObject prefabToClone, string newName = null);

            public void FixMixerGroups(GameObject prefab);
            public void RegisterItem(Item item, bool isScrap, bool IsBuyableItem, int rarity, string terminalDescription);
            public void RemoveScrapFromLevels(Item scrapItem, LevelTypes levelFlags = LevelTypes.None, string[] levelOverrides = null);

            public void RemoveShopItem(Item shopItem);
        }

        internal class LethalLibScrapManagement : IScrapManagement
        {

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            public GameObject CloneNetworkPrefab(GameObject prefabToClone, string newName = null)
            {
                return LethalLib.Modules.NetworkPrefabs.CloneNetworkPrefab(prefabToClone, newName);
            }

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            public void FixMixerGroups(GameObject prefab)
            {
                Utilities.FixMixerGroups(prefab);
            }

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            public void RegisterItem(Item item, bool isScrap, bool IsBuyableItem, int rarity, string terminalDescription)
            {
                if(!isScrap && !IsBuyableItem)
                {
                    Items.RegisterItem(item);
                    return;
                }
                if (isScrap)
                {
                    Items.RegisterScrap(item, rarity, Levels.LevelTypes.All);
                }
                if (IsBuyableItem)
                {
                    var terminalNode = ScriptableObject.CreateInstance<TerminalNode>();
                    terminalNode.displayText = terminalDescription;
                    Items.RegisterShopItem(item, null, null, terminalNode, item.creditsWorth);
                }
                    
            }

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            public void RemoveScrapFromLevels(Item scrapItem, LevelTypes levelFlags = LevelTypes.None, string[] levelOverrides = null)
            {
                Items.RemoveScrapFromLevels(scrapItem, ConvertToLethalLibLevelTypes(levelFlags), levelOverrides);
            }

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            public void RemoveShopItem(Item shopItem)
            {
                Items.RemoveShopItem(shopItem);
            }

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            private Levels.LevelTypes ConvertToLethalLibLevelTypes(LevelTypes levelType)
            {
                if (Enum.IsDefined(typeof(Levels.LevelTypes), levelType))
                {
                    return (Levels.LevelTypes)levelType;
                }
                return Levels.LevelTypes.None;
            }
        }


        internal class LethalLevelLoaderScrapManagement : IScrapManagement
        {
            ScriptableObject LittleCompanyMod;

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            internal LethalLevelLoaderScrapManagement()
            {
                LittleCompanyMod = ExtendedMod.Create("LittleCompany", "Toybox");
            }

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            public GameObject CloneNetworkPrefab(GameObject prefabToClone, string newName = null)
            {
                return CloneNetworkPrefabImpl(prefabToClone, newName);
            }

            public void FixMixerGroups(GameObject prefab)
            {
                // Nothing to do here probably
            }

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            public void RegisterItem(Item item, bool isScrap, bool IsBuyableItem, int rarity, string terminalDescription)
            {
                item.isScrap = isScrap;
                ExtendedItem ex = ExtendedItem.Create(item, (ExtendedMod) LittleCompanyMod, ContentType.Custom);
                ex.IsBuyableItem = IsBuyableItem;
                if (terminalDescription != null)
                    ex.OverrideInfoNodeDescription = terminalDescription;
                if (rarity != -1)
                {
                    LevelMatchingProperties levelMatchingProperties = LevelMatchingProperties.Create(ex);
                    levelMatchingProperties.levelTags.Add(new StringWithRarity("Vanilla", rarity));
                    levelMatchingProperties.levelTags.Add(new StringWithRarity("Custom", rarity));
                    ex.LevelMatchingProperties = levelMatchingProperties;
                }
            }

            public void RemoveScrapFromLevels(Item scrapItem, LevelTypes levelFlags = LevelTypes.None, string[] levelOverrides = null)
            {
                // Not supported yet?, sucks to be you, don't reuse the same save with a different config
            }

            public void RemoveShopItem(Item shopItem)
            {
                // Not supported yet?, sucks to be you, don't reuse the same save with a different config
            }

            // Reimplementation of CloneNetworkPrefab since it doesnt exist in LethalLevelLoader
            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            public static GameObject CloneNetworkPrefabImpl(GameObject prefabToClone, string newName = null)
            {
                GameObject gameObject = PrefabCloner.ClonePrefab(prefabToClone, newName);
                LethalLevelLoaderNetworkManager.RegisterNetworkPrefab(gameObject);
                return gameObject;
            }
        }
    }
}
