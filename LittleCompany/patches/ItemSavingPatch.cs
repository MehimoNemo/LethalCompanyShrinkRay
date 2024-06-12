using HarmonyLib;
using LittleCompany.components;
using LittleCompany.modifications;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LittleCompany.patches
{
    [HarmonyPatch]
    internal class ItemSavingPatch
    {
        [HarmonyPatch(typeof(GameNetworkManager), "SaveItemsInShip")]
        [HarmonyPrefix]
        public static void SaveItemsInShip()
        {
            GrabbableObject[] array = UnityEngine.Object.FindObjectsByType<GrabbableObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            List<float> itemSizes = new List<float>();
            foreach (var item in array)
            {
                Plugin.Log("SaveItemsInShip -> " + item.name);
                if (item.TryGetComponent(out ItemScaling scaling))
                    scaling.ResetItemProperties();

                if (item.itemProperties.spawnPrefab != null && !item.itemUsedUp && StartOfRound.Instance.allItemsList.itemsList.Contains(item.itemProperties))
                {
                    var itemScale = scaling != null ? scaling.RelativeScale : 1f;
                    itemSizes.Append(itemScale);
                    Plugin.Log("Saved item scale of " + itemScale + " for item " + item.name);
                }
            }

            Plugin.Log("Saving scales for " + GameNetworkManager.Instance.currentSaveFileName);
            ES3.Save("shipGrabbableItemScale", itemSizes.ToArray(), GameNetworkManager.Instance.currentSaveFileName);
        }

        [HarmonyPatch(typeof(StartOfRound), "LoadShipGrabbableItems")]
        [HarmonyPostfix]
        public static void LoadShipGrabbableItems()
        {
            if (!ES3.KeyExists("shipGrabbableItemScale", GameNetworkManager.Instance.currentSaveFileName)) return;

            float[] itemScales = ES3.Load<float[]>("shipGrabbableItemScale", GameNetworkManager.Instance.currentSaveFileName);
            Plugin.Log("Loading " + itemScales.Length + " scales for " + GameNetworkManager.Instance.currentSaveFileName);
            //List<int> itemIDs = ES3.Load<int[]>("shipGrabbableItemIDs", GameNetworkManager.Instance.currentSaveFileName).ToList();
            if (itemScales.Length == 0) return;

            GrabbableObject[] items = UnityEngine.Object.FindObjectsByType<GrabbableObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            Plugin.Log("Found items: " + items.Length);

            for (int i = 0; i < items.Length; i++)
            {
                if (i >= itemScales.Length) break;

                ObjectModification.ScalingOf(items[i]).ScaleTo(itemScales[i], null);
                Plugin.Log("Item " + items[i].name + " scaled to " + itemScales[i]);
            }
        }
    }
}
