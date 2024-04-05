using GameNetcodeStuff;
using LittleCompany.helper;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LittleCompany.compatibility
{
    internal class ModelReplacementApiCompatibility
    {
        public const string ModelReplacementApiReferenceChain = "meow.ModelReplacementAPI";

        private static bool? _enabled;

        public static bool enabled
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

        public ModelReplacementApiCompatibility(PlayerControllerB pcb)
        {
            player = pcb;
            ReloadCurrentReplacementModel();
        }
        
        public void ReloadCurrentReplacementModel()
        {
            if (enabled)
            {
                GameObject foundModel = FindCurrentReplacementModel();
                if (foundModel != null)
                {
                    if(replacementModel == null || foundModel.name != replacementModel.name)
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
        }

        public void AdjustToSize(float size)
        {
            if(enabled && replacementModel != null && replacementModel.transform != null)
            {
                Plugin.Log("Adjust to: " + size);
                replacementModel.transform.localScale = replacementModelOriginalScale * size;
            }
        }

        private GameObject FindCurrentReplacementModel()
        {
            string matchName = "(Clone)(" + player.playerUsername + ")";
            foreach (GameObject g in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (g.name.EndsWith(matchName))
                {
                    return g;
                }
            }
            return null;
        }
    }
}
