using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.Netcode;
using LittleCompany.components;
using System;

namespace LittleCompany.helper
{
    internal class ItemInfo
    {
        public enum VanillaItem
        {
            Custom,
            Binoculars,
            Boombox,
            clipboard,
            Homemade_flashbang,
            Extension_ladder,
            Flashlight,
            Ammo,
            Jetpack,
            Key,
            Lockpicker,
            Apparatus,
            Mapper,
            Pro_flashlight,
            Radar_booster,
            Body,
            Magic_7_ball,
            Airhorn,
            Bell,
            Big_bolt,
            Bottles,
            Brush,
            Candy,
            Cash_register,
            Chemical_jug,
            Clown_horn,
            Large_axle,
            Comedy,
            Teeth,
            Dust_pan,
            Easter_egg,
            Egg_beater,
            V_type_engine,
            Golden_cup,
            Fancy_lamp,
            Painting,
            Plastic_fish,
            Laser_pointer,
            Flask,
            Gift,
            Gold_bar,
            Hairdryer,
            Kitchen_knife,
            Magnifying_glass,
            Metal_sheet,
            Cookie_mold_pan,
            Mug,
            Perfume_bottle,
            Old_phone,
            Jar_of_pickles,
            Pill_bottle,
            Hive,
            Remote,
            Ring,
            Toy_robot,
            Rubber_Ducky,
            Red_soda,
            Steering_wheel,
            Stop_sign,
            Tea_kettle,
            Toothpaste,
            Toy_cube,
            Tragedy,
            Whoopie_cushion,
            Yield_sign,
            Shotgun,
            Shovel,
            Spray_paint,
            Sticky_note,
            Stun_grenade,
            TZP_Inhalant,
            Walkie_talkie,
            Zap_gun
        }

        public static VanillaItem itemTypeByName(string name)
        {
            if (name == null || name.Length == 0) return VanillaItem.Custom;

            name = name.Replace("-", "_");
            name = name.Replace(" ", "_");

            if (Enum.TryParse(typeof(VanillaItem), name, true, out object item))
                return (VanillaItem)item;
            return VanillaItem.Custom;
        }

        public static readonly List<Item> SpawnableItems = Resources.FindObjectsOfTypeAll<Item>().Where(item => item.itemName != GrabbablePlayerObject.Name).ToList();
        public static Item itemByName(string name)
        {
            return SpawnableItems.Find(x => x.name == name);
        }

        public static GameObject visualCopyOf(Item item)
        {
            var copy = UnityEngine.Object.Instantiate(item.spawnPrefab);

            if (copy.TryGetComponent(out NetworkObject networkObject))
                UnityEngine.Object.DestroyImmediate(networkObject);
            if (copy.TryGetComponent(out GrabbableObject grabbableObject))
                UnityEngine.Object.DestroyImmediate(grabbableObject);
            if (copy.TryGetComponent(out Collider collider))
                UnityEngine.Object.DestroyImmediate(collider);

            if(!copy.TryGetComponent(out ScanNodeProperties scanNode))
                scanNode = copy.GetComponentInChildren<ScanNodeProperties>();
            if(scanNode != null)
                UnityEngine.Object.DestroyImmediate(scanNode);

            return copy;
        }

    }
}
