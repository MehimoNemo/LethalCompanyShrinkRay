using GameNetcodeStuff;
using LittleCompany.components;
using LittleCompany.helper;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LittleCompany.compatibility
{
    [DisallowMultipleComponent]
    internal class ModelReplacementApiCompatibilityComponent : MonoBehaviour, IScalingListener
    {
        public const string ModelReplacementApiReferenceChain = "meow.ModelReplacementAPI";

        private static bool? _enabled;

        public static bool compatEnabled
        {
            get
            {
                if (_enabled == null)
                {
                    _enabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(ModelReplacementApiReferenceChain);
                }
                return (bool)_enabled;
            }
        }

        internal PlayerControllerB player;
        internal GameObject replacementModel;
        internal Vector3 replacementModelOriginalScale;

        public void Awake()
        {
            player = GetComponent<PlayerControllerB>();
            ReloadCurrentReplacementModel();
            GetComponent<PlayerScaling>()?.AddListener(this);
        }

        public void OnDestroy()
        {
            GetComponent<PlayerScaling>()?.RemoveListener(this);
        }

        public void ReloadCurrentReplacementModel()
        {
            GameObject foundModel = FindCurrentReplacementModel();
            if (foundModel != null)
            {
                if (replacementModel == null || foundModel.name != replacementModel.name)
                {
                    Plugin.Log("Replace original scale");
                    replacementModelOriginalScale = foundModel.transform.localScale;
                }
                replacementModel = foundModel;
                Plugin.Log("Replacement: " + replacementModel.name);
                AdjustToSize(PlayerInfo.SizeOf(player));
            }
            else
            {
                Plugin.Log("Replacement null");
                replacementModel = null;
            }
        }

        public void AdjustToSize(float size)
        {
            if (replacementModel != null && replacementModel.transform != null)
            {
                replacementModel.transform.localScale = replacementModelOriginalScale * size;
            }
        }

        private GameObject FindCurrentReplacementModel()
        {
            string matchName = "(Clone)(" + player.playerUsername + ")";
            HashSet<GameObject> found = new HashSet<GameObject>();
            foreach (GameObject g in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (g.name.EndsWith(matchName))
                {
                    found.Add(g);
                }
            }
            // If there's only one then the model didn't change
            if(found.Count == 1)
            {
                return found.First();
            }
            // If there's multiple then we need to return the new model (the old one will get destroyed at the end of the frame)
            foreach (GameObject g in found)
            {
               if(g.name != replacementModel.name)
               {
                    return g;
               }
            }
            // No models
            return null;
        }

        public void AfterEachScale(float from, float to, PlayerControllerB playerBy)
        {
            AdjustToSize(to);
        }

        public void AtEndOfScaling(float from, float to, PlayerControllerB playerB)
        {
            ReloadCurrentReplacementModel();
        }
    }
}
