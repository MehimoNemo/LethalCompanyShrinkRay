using GameNetcodeStuff;

namespace LCShrinkRay.helper
{
    internal class PlayerDefaultValues
    {
        public struct DefaultValues
        {
            public float jumpForce { get; set; }
            public float sprintMultiplier { get; set; }
        }

        private static bool initialized = false;
        private static DefaultValues defaultValues;
        public static DefaultValues? Values
        {
            get
            {
                return initialized ? defaultValues : null;
            }
        }

        public static void Init(PlayerControllerB __instance, float ___sprintMultiplier, float ___jumpForce)
        {
            if (initialized) return;

            defaultValues = new DefaultValues
            {
                jumpForce = ___jumpForce,
                sprintMultiplier = ___sprintMultiplier
            };

            Plugin.Log("Loaded default values: " + defaultValues.ToString());
            initialized = true;
        }
    }
}
