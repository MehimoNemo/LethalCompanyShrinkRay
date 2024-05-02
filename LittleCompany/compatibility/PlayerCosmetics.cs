using GameNetcodeStuff;
using LittleCompany.helper;
using System;
using System.Linq;
using UnityEngine;

namespace LittleCompany.compatibility
{
    internal class PlayerCosmetics
    {
        internal static string[] BasePartsException => ["commando(Clone)", "ArmsRotationTarget(Clone)", "scavEmoteSkeleton(Clone)"];
        internal static string[] ModdedPartsCompatibilityException = ["LocalPhoneModel(Clone)"];

        public static void RegularizeCosmetics()
        {
            Plugin.Log("RegularizeCosmetics");
            foreach (var player in PlayerInfo.AllPlayers)
            {
                Component cosmetic = GetCosmeticApplication(player);

                if (cosmetic != null)
                {
                    Transform[] componentsInChildren = cosmetic.gameObject.GetComponentsInChildren<Transform>();
                    foreach (Transform transform in componentsInChildren)
                    {
                        if (transform.name.Contains("(Clone)") && !BasePartsException.Contains(transform.name) && !ModdedPartsCompatibilityException.Contains(transform.name))
                        {
                            transform.localScale = new Vector3(0.38f, 0.38f, 0.38f);
                        }
                    }
                }
            }
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
