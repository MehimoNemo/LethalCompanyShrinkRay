using GameNetcodeStuff;
using System;
using System.Linq;
using UnityEngine;

namespace LittleCompany.compatibility
{
    internal class PlayerCosmetics
    {
        internal static string[] BasePartsException => ["commando(Clone)", "ArmsRotationTarget(Clone)", "scavEmoteSkeleton(Clone)"];

        public static void RegularizeCosmetics()
        {
            Plugin.Log("RegularizeCosmetics");
            PlayerControllerB[] players = StartOfRound.Instance.allPlayerScripts;
            foreach (var player in players)
            {
                Component cosmetic = GetCosmeticApplication(player);

                if (cosmetic != null)
                {
                    Plugin.Log("Cosmetic re-synch");

                    Transform[] componentsInChildren = cosmetic.gameObject.GetComponentsInChildren<Transform>();
                    foreach (Transform transform in componentsInChildren)
                    {
                        if (transform.name.Contains("(Clone)") && !BasePartsException.Contains(transform.name))
                        {
                            transform.localScale = new Vector3(0.38f, 0.38f, 0.38f);
                        }
                    }
                }
                else
                {
                    Plugin.Log("No cosmetics to re-synch");
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
