using GameNetcodeStuff;
using UnityEngine;
using static LittleCompany.events.item.ItemEventManager;

namespace LittleCompany.events.item
{
    internal class FlashlightEventHandler : ItemEventHandler
    {
        private static float flashlightBulbDefaultIntensity = 0f;
        private static float flashlightBulbGlowDefaultIntensity = 0f;
        private FlashlightItem flashlight;

        public override void OnAwake()
        {
            base.OnAwake();
            flashlight = item as FlashlightItem;
        }
        public override void Scaled(float from, float to, PlayerControllerB playerShrunkenBy)
        {
            base.Scaled(from, to, playerShrunkenBy);

            if (Mathf.Approximately(flashlightBulbDefaultIntensity, 0f))
                flashlightBulbDefaultIntensity = flashlight.flashlightBulb.intensity;
            flashlight.flashlightBulb.intensity = flashlightBulbDefaultIntensity * to;

            if (Mathf.Approximately(flashlightBulbGlowDefaultIntensity, 0f))
                flashlightBulbGlowDefaultIntensity = flashlight.flashlightBulbGlow.intensity;
            flashlight.flashlightBulbGlow.intensity = flashlightBulbGlowDefaultIntensity * to;
        }
    }
}
