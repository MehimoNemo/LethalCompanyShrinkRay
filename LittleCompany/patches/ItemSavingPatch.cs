using HarmonyLib;
using LittleCompany.components;
using LittleCompany.modifications;
using System;
using System.Collections;
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
            if (StartOfRound.Instance.isChallengeFile) return;

            GrabbableObject[] array = UnityEngine.Object.FindObjectsByType<GrabbableObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            int scaledObjectCount = 0;
            List<float> itemSizes = new List<float>();
            foreach (var item in array)
            {
                if (item.TryGetComponent(out ItemScaling scaling))
                    scaling.ResetItemProperties();

                if (item.itemProperties.spawnPrefab != null && !item.itemUsedUp && StartOfRound.Instance.allItemsList.itemsList.Contains(item.itemProperties))
                {
                    var isScaled = scaling != null && !scaling.Unchanged;
                    if (isScaled)
                        scaledObjectCount++;

                    var itemScale = isScaled ? scaling.RelativeScale : 1f;
                    itemSizes.Add(itemScale);
                    Plugin.Log("Saved item scale of " + itemScale + " for item " + item.name);
                }
            }

            if (scaledObjectCount > 0)
            {
                Plugin.Log("Saving " + scaledObjectCount + " scaled items (including " + (itemSizes.Count - scaledObjectCount) + " unscaled items).");
                ES3.Save("shipGrabbableItemScale", itemSizes.ToArray(), GameNetworkManager.Instance.currentSaveFileName);
            }
            else
            {
                Plugin.Log("No scaled items. Nothing to save.");
                ES3.DeleteKey("shipGrabbableItemScale", GameNetworkManager.Instance.currentSaveFileName);
            }
        }

        [HarmonyPatch(typeof(StartOfRound), "LoadShipGrabbableItems")]
        [HarmonyPostfix]
        public static void LoadShipGrabbableItems(StartOfRound __instance)
        {
            if (!ES3.KeyExists("shipGrabbableItemScale", GameNetworkManager.Instance.currentSaveFileName)) return;

            __instance.StartCoroutine(ApplyScalesLater());
        }

        private static IEnumerator ApplyScalesLater()
        {
            yield return new WaitForSeconds(0.5f);

            float[] itemScales = ES3.Load<float[]>("shipGrabbableItemScale", GameNetworkManager.Instance.currentSaveFileName);
            Plugin.Log("Loading " + itemScales.Length + " scaled items.");
            List<int> itemIDs = ES3.Load<int[]>("shipGrabbableItemIDs", GameNetworkManager.Instance.currentSaveFileName).ToList();
            if (itemScales.Length == 0) yield break;

            List<GrabbableObject> items = UnityEngine.Object.FindObjectsByType<GrabbableObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).ToList();
            Plugin.Log("Found items: " + items.Count);

            for (int i = 0; i < itemIDs.Count; i++)
            {
                if (itemIDs[i] >= StartOfRound.Instance.allItemsList.itemsList.Count || Mathf.Approximately(itemScales[i], 1f)) continue;
                var itemID = StartOfRound.Instance.allItemsList.itemsList[itemIDs[i]].itemId;
                var foundItem = items.FirstOrDefault((item) => item.itemProperties.itemId == itemID);
                if(foundItem == null) continue;

                Plugin.Log("Item " + foundItem.name + " scaled to " + itemScales[i]);
                ItemModification.ScalingOf(foundItem).ScaleToImmediate(itemScales[i], null);

                items.Remove(foundItem); // Don't scale it another time
            }
        }
    }
}
