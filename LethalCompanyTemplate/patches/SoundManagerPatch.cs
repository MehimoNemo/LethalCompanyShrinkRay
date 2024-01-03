using HarmonyLib;
using LCShrinkRay.comp;

namespace LCShrinkRay.patches
{
    [HarmonyPatch]
    class SoundManagerPatch
    {
        [HarmonyPatch(typeof(SoundManager), "SetPlayerPitch")]
        [HarmonyPostfix]
        public static void Postfix(float pitch, int playerObjNum)
        {
            Shrinking.Instance.SetPlayerPitch(pitch, (ulong)playerObjNum);
        }
    }
}