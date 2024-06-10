using System;
using System.Collections.Generic;
using UnityEngine;
using LittleCompany.components;
using static LittleCompany.helper.ItemInfo;

namespace LittleCompany.events.item
{
    internal class ItemEventManager
    {
        public static readonly Dictionary<VanillaItem, Type> EventHandler = new Dictionary<VanillaItem, Type>
        {
            { VanillaItem.Custom,         typeof(CustomItemEventHandler) },
            { VanillaItem.Pro_flashlight, typeof(FlashlightEventHandler) },
            { VanillaItem.Flashlight,     typeof(FlashlightEventHandler) },
            //{ VanillaItem.Laser_pointer,  typeof(FlashlightEventHandler) }, // WIP
            { VanillaItem.Gift,           typeof(GiftBoxEventHandler)    },
            { VanillaItem.Shovel,         typeof(ShovelEventHandler)     },
            { VanillaItem.Key,            typeof(KeyEventHandler)        },
            { VanillaItem.Clown_horn,     typeof(HornEventHandler)       },
            { VanillaItem.Airhorn,        typeof(HornEventHandler)       }
        };

        public static Type EventHandlerTypeByName(string name) => EventHandler.GetValueOrDefault(itemTypeByName(name), typeof(CustomItemEventHandler));

        public static ItemEventHandler EventHandlerOf(GrabbableObject item)
        {
            var eventHandlerType = EventHandlerTypeByName(item.itemProperties.itemName);
            Plugin.Log("Found eventHandler with name " + eventHandlerType.ToString() + " for enemy name " + item.itemProperties.itemName);
            if (item.TryGetComponent(eventHandlerType, out Component eventHandler))
                return eventHandler as ItemEventHandler;

            Plugin.Log("Enemy had no event handler!", Plugin.LogType.Error);
            return null;
        }

        public static void BindAllEventHandler()
        {
            int handlersAdded = 0;
            int customHandlersAdded = 0;
            foreach (var item in SpawnableItems)
            {
                if (item.spawnPrefab == null)
                {
                    Plugin.Log("Item " + item.itemName + " had no spawnPrefab. Unable to connect item event handler.", Plugin.LogType.Warning);
                    continue;
                }

                var eventHandlerType = EventHandlerTypeByName(item.itemName);
                var eventHandler = item.spawnPrefab.AddComponent(eventHandlerType);
                if (eventHandler != null)
                    handlersAdded++;
#if DEBUG
                if (eventHandler != null)
                {
                    if (eventHandlerType == typeof(CustomItemEventHandler))
                        customHandlersAdded++;
                    else
                        Plugin.Log("Added event handler \"" + eventHandlerType.Name + "\" for item \"" + item.itemName + "\"");
                }
                else
                    Plugin.Log("No enemy handler found for item \"" + item.itemName + "\"");
#endif
            }

            if (customHandlersAdded > 0)
                Plugin.Log("Added custom event handler for " + customHandlersAdded + " items");

            Plugin.Log("BindAllItemEvents -> Added handler for " + handlersAdded + "/" + SpawnableItems.Count + " items.");
        }

        public class ItemEventHandler : EventHandlerBase
        {
            internal GrabbableObject item = null;

            public override void OnAwake()
            {
                item = GetComponent<GrabbableObject>();
                GetComponent<ItemScaling>()?.AddListener(this);
            }

            public override void DestroyObject()
            {
                item.DestroyObjectInHand(item.playerHeldBy);
            }

            public override void DespawnObject()
            {
                item.NetworkObject.Despawn();
            }
        }
    }
}
