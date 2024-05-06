using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.Netcode;

namespace LittleCompany.helper
{
    internal class ItemInfo
    {
        // EnlargingPotionItem
        // ShrinkRayItem
        // ShrinkingPotionItem
        // grabbablePlayerItem
        // Binoculars
        // Boombox
        // Clipboard
        // DiyFlashbang
        // ExtensionLadder
        // Flashlight
        // GunAmmo
        // Jetpack
        // Key
        // LockPicker
        // LungApparatus
        // MapDevice
        // ProFlashlight
        // RadarBooster
        // Ragdoll
        // 7Ball
        // Airhorn
        // Bell
        // BigBolt
        // BottleBin
        // Brush
        // Candy
        // CashRegister
        // ChemicalJug
        // ClownHorn
        // Cog1
        // ComedyMask
        // Dentures
        // DustPan
        // EasterEgg
        // EggBeater
        // EnginePart1
        // FancyCup
        // FancyLamp
        // FancyPainting
        // FishTestProp
        // FlashLaserPointer
        // Flask
        // GiftBox
        // GoldBar
        // Hairdryer
        // Knife
        // MagnifyingGlass
        // MetalSheet
        // MoldPan
        // Mug
        // PerfumeBottle
        // Phone
        // PickleJar
        // PillBottle
        // RedLocustHive
        // Remote
        // Ring
        // RobotToy
        // RubberDuck
        // SodaCanRed
        // SteeringWheel
        // StopSign
        // TeaKettle
        // Toothpaste
        // ToyCube
        // TragedyMask
        // WhoopieCushion
        // YieldSign
        // Shotgun
        // Shovel
        // SprayPaint
        // StickyNote
        // StunGrenade
        // TZPInhalant
        // WalkieTalkie
        // ZapGun
        // CardboardBox
        public static readonly List<Item> SpawnableItems = Resources.FindObjectsOfTypeAll<Item>().ToList();

        public static GameObject visualCopyOf(Item item)
        {
            var copy = Object.Instantiate(item.spawnPrefab);

            if (copy.TryGetComponent(out NetworkObject networkObject))
                Object.DestroyImmediate(networkObject);
            if (copy.TryGetComponent(out GrabbableObject grabbableObject))
                Object.DestroyImmediate(grabbableObject);
            if (copy.TryGetComponent(out Collider collider))
                Object.DestroyImmediate(collider);

            if(!copy.TryGetComponent(out ScanNodeProperties scanNode))
                scanNode = copy.GetComponentInChildren<ScanNodeProperties>();
            if(scanNode != null)
                Object.DestroyImmediate(scanNode);

            return copy;
        }
    }
}
