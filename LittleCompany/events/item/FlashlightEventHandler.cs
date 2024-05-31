using GameNetcodeStuff;
using UnityEngine;
using static LittleCompany.events.item.ItemEventManager;

namespace LittleCompany.events.item
{
    internal class FlashlightEventHandler : ItemEventHandler
    {
        float baseBatteryUsage = 0f;

        public override void OnAwake()
        {
            base.OnAwake();
        }
        public override void Scaling(float from, float to, PlayerControllerB playerShrunkenBy)
        {
            base.Scaled(from, to, playerShrunkenBy);

            if (Mathf.Approximately(baseBatteryUsage, 0f))
                baseBatteryUsage = item.itemProperties.batteryUsage;

            var batteryUsageMultiplier = 1f + Mathf.Max((to - 1f) * 5, -0.9f);
            item.itemProperties.batteryUsage = baseBatteryUsage * batteryUsageMultiplier;
        }
    }
}
