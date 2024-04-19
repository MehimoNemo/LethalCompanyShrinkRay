using LethalLib.Modules;
using System;
using UnityEngine;

namespace LittleCompany.dependency
{
    public static class ScrapManagementFacade
    {
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

        public static GameObject CloneNetworkPrefab(GameObject prefabToClone, string newName = null)
        {
            return NetworkPrefabs.CloneNetworkPrefab(prefabToClone, newName);
        }

        public static void FixMixerGroups(GameObject prefab)
        {
            Utilities.FixMixerGroups(prefab);
        }

        public static void RegisterScrap(Item spawnableItem, int rarity, LevelTypes levelFlags)
        {
            Items.RegisterScrap(spawnableItem, rarity, ConvertLevelTypes(levelFlags));
        }

        public static void RegisterItem(Item plainItem)
        {
            Items.RegisterItem(plainItem);
        }

        public static void RegisterShopItem(Item shopItem, TerminalNode buyNode1 = null, TerminalNode buyNode2 = null, TerminalNode itemInfo = null, int price = -1)
        {
            Items.RegisterShopItem(shopItem, buyNode1, buyNode2, itemInfo, price);
        }

        public static void RemoveScrapFromLevels(Item scrapItem, LevelTypes levelFlags = LevelTypes.None, string[] levelOverrides = null)
        {
            Items.RemoveScrapFromLevels(scrapItem, ConvertLevelTypes(levelFlags), levelOverrides);
        }

        public static void RemoveShopItem(Item shopItem)
        {
            Items.RemoveShopItem(shopItem);
        }

        private static Levels.LevelTypes ConvertLevelTypes(LevelTypes levelType)
        {
            if (Enum.IsDefined(typeof(Levels.LevelTypes), levelType))
            {
                return (Levels.LevelTypes)levelType;
            }
            return Levels.LevelTypes.None;
        }
    }
}
