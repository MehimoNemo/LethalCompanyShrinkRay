using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace LCShrinkRay.patches
{
    [Harmony]

    [HarmonyPatch(typeof(SoundManager), "SetPlayerPitch")]
    class SoundManagerPatch
    {
        private static ManualLogSource mls;
        static SoundManager dibble = SoundManager.Instance;


        // This method will be called before the original SetPlayerPitch method
        static void Prefix(float pitch, int playerObjNum)
        {
            if (dibble != null)
            {


                // Your custom logic here to get the variable from Shrinking.cs
                string myPlayerObjectName = "Player";
                if (playerObjNum != 0)
                {
                    myPlayerObjectName = "Player (" + playerObjNum.ToString() + ")";
                }
                GameObject myPlayerObject = GameObject.Find(myPlayerObjectName);
                float myScale = myPlayerObject.GetComponent<Transform>().localScale.x;
                float customPitch = pitch + -0.417f * myScale + 0.417f;

                
                mls = BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_GUID);
                mls.LogInfo("SUCCESSFULLY RUNNING PITCH FROM PATCH");
                //SoundManager.Instance.SetPlayerPitch(customPitch, playerObjNum);
                dibble.diageticMixer.SetFloat($"PlayerPitch{playerObjNum}", customPitch);
            }
            else
            {
                dibble = SoundManager.Instance;
            }
        }
    }

}
