using GameNetcodeStuff;
using LittleCompany.components;
using LittleCompany.helper;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LittleCompany.compatibility
{
    internal class LethalVRMCompatibilityComponent : MonoBehaviour, IScalingListener
    {
        public const string LethalVRMApiReferenceChain = "Ooseykins.LethalVRM";
        public const string BetterLethalVRMApiReferenceChain = "OomJan.BetterLethalVRM";

        private static bool? _enabled;

        public static bool compatEnabled
        {
            get
            {
                if (_enabled == null)
                {
                    _enabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(LethalVRMApiReferenceChain)
                        || BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(BetterLethalVRMApiReferenceChain);
                }
                return (bool)_enabled;
            }
        }

        internal PlayerControllerB player;
        internal GameObject replacementVRM;
        internal Vector3 replacementVRMOriginalScale;
        internal int frame = 0;

        public void Awake()
        {
            player = GetComponent<PlayerControllerB>();
            MatchVRM();
            GetComponent<PlayerScaling>()?.AddListener(this);
        }

        public void OnDestroy()
        {
            GetComponent<PlayerScaling>()?.RemoveListener(this);
        }

        public void LateUpdate()
        {
            if(replacementVRM == null && frame < 1000)
            {
                frame++;
                if (frame % 5 == 0)
                {
                    // Trying to match VRMs every 5 frames for the first 1000 frames (About 16 sec)
                    MatchVRM();
                    ResetSizeToPlayerSize();
                }
            }
        }

        void MatchVRM()
        {
            string matchName = "LethalVRM Character Model " + player.playerUsername + " " + player.playerSteamId;
            foreach (GameObject g in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (g.name.EndsWith(matchName))
                {
                    replacementVRM = g;
                    replacementVRMOriginalScale = replacementVRM.transform.localScale;
                    return;
                }
            }
        }

        public void AdjustToSize(float size)
        {
            if (replacementVRM != null && replacementVRM.transform != null)
            {
                replacementVRM.transform.localScale = replacementVRMOriginalScale * size;
            }
        }

        public void ResetSizeToPlayerSize()
        {
            if (player != null)
            {
                AdjustToSize(PlayerInfo.SizeOf(player));
            }
        }

        public void AfterEachScale(float from, float to, PlayerControllerB playerBy)
        {
            AdjustToSize(to);
        }

        public void AtEndOfScaling()
        {
            ResetSizeToPlayerSize();
        }
    }
}
