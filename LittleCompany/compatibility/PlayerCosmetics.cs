using GameNetcodeStuff;
using LittleCompany.helper;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LittleCompany.compatibility
{
    internal class PlayerCosmetics
    {
        internal static string[] BasePartsException => ["commando(Clone)", "ArmsRotationTarget(Clone)", "scavEmoteSkeleton(Clone)"];
        internal static string[] ModdedPartsCompatibilityException = ["LocalPhoneModel(Clone)"];

        public static void RegularizeAllCosmeticsWhenLoading()
        {
            Plugin.Log("RegularizeCosmetics");
            foreach (var player in PlayerInfo.AllPlayers)
            {
                RegularizePlayerCosmetics(player);
            }
        }
        public static void RegularizePlayerCosmetics(PlayerControllerB player)
        {
            foreach (Transform cosmetic in GetAllCosmeticsOfPlayer(player))
            {
                cosmetic.localScale = cosmetic.localScale * PlayerInfo.SizeOf(player);
            }
        }

        private static List<Transform> GetAllCosmeticsOfPlayer(PlayerControllerB player)
        {
            Component cosmetic = GetCosmeticApplication(player);
            if (cosmetic == null) return [];

            Transform[] componentsInChildren = cosmetic.gameObject.GetComponentsInChildren<Transform>();
            List<Transform> cosmetics = [];
            foreach (Transform transform in componentsInChildren)
            {
                if (transform.name.Contains("(Clone)") && !BasePartsException.Contains(transform.name) && !ModdedPartsCompatibilityException.Contains(transform.name))
                {
                    cosmetics.Add(transform);
                }
            }
            return cosmetics;
        }

        public static Component GetCosmeticApplication(PlayerControllerB playerControllerB)
        {
            Component[] components = playerControllerB.transform.Find("ScavengerModel").Find("metarig").GetComponents(typeof(Component));
            foreach (Component component in components)
            {
                if (component.GetType().FullName.Equals("MoreCompany.Cosmetics.CosmeticApplication"))
                {
                    return component;
                }
            }
            return null;
        }
    }
}
