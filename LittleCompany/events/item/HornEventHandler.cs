using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;
using static LittleCompany.events.item.ItemEventManager;

namespace LittleCompany.events.item
{
    [HarmonyPatch]
    internal class HornEventHandler : ItemEventHandler
    {
        NoisemakerProp noiseMaker = null;
        private float defaultMinPitch = 0f;
        private float defaultMaxPitch = 0f;

        public override void OnAwake()
        {
            base.OnAwake();
            noiseMaker = item as NoisemakerProp;
            if (noiseMaker != null)
            {
                defaultMinPitch = noiseMaker.minPitch;
                defaultMaxPitch = noiseMaker.maxPitch;
            }
        }

        public override void Scaling(float from, float to, PlayerControllerB playerShrunkenBy)
        {
            base.Scaling(from, to, playerShrunkenBy);

            if (noiseMaker != null)
            {
                float pitchDiff;
                var sizeDiff = to - 1f;
                if (sizeDiff < 0f) // Smaller
                    pitchDiff = sizeDiff / 2f;
                else
                    pitchDiff = Mathf.Min(sizeDiff / 10f, 0.5f);
                noiseMaker.minPitch = defaultMinPitch - pitchDiff;
                noiseMaker.maxPitch = defaultMaxPitch - pitchDiff;
            }
        }
    }
}
