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
        internal static Dictionary<String, Vector3> DefaultScale = [];

        public static void RegularizeCosmetics()
        {
            Plugin.Log("RegularizeCosmetics");
            foreach (var player in PlayerInfo.AllPlayers)
            {
                foreach (Transform cosmetic in GetAllCosmeticsOfPlayer(player))
                {
                    if (!DefaultScale.ContainsKey(cosmetic.name))
                    {
                        DefaultScale[cosmetic.name] = cosmetic.localScale;
                    }
                    cosmetic.localScale = DefaultScale[cosmetic.name];
                }
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
