using LCShrinkRay.comp;
using LCShrinkRay.Config;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace LCShrinkRay.coroutines
{
    internal class SetPlayerPitch : MonoBehaviour
    {
        public GameObject playerObj { get; private set; }

        public static void StartRoutine(ulong playerID, float pitch)
        {
            var playerObj = Shrinking.GetPlayerObject(playerID);
            var routine = playerObj.AddComponent<SetPlayerPitch>();
            routine.playerObj = playerObj;
            routine.StartCoroutine(routine.run(pitch, playerID));
        }

        private IEnumerator run(float pitch, ulong playerID)
        {
            // Check if the player object is valid
            if (playerObj == null)
            {
                Plugin.log("PLAYEROBJECT IS NULL", Plugin.LogType.Warning);
                yield break;
            }

            Plugin.log("SUCCESSFULLY RUNNING PITCH FROM PATCH");

            float elapsedTime = 0f;

            // Get the player object's scale
            float scale = playerObj.transform.localScale.x;
            if (playerObj.transform == null)
            {
                Plugin.log("PLAYEROBJECT.TRANSFORM IS NULL", Plugin.LogType.Warning);
                yield break;
            }

            //float modifiedPitch = 1f;
            //float modifiedPitch = -0.417f * scale + 1.417f;
            Shrinking.Instance.myScale = Shrinking.GetPlayerObject(Shrinking.Instance.clientId).transform.localScale.x;

            float intensity = -1f * (float)ModConfig.Instance.values.pitchDistortionIntensity;
            float modifiedPitch = (intensity * (scale - Shrinking.Instance.myScale) + 1f) * pitch;

            // Set the modified pitch using the original method
            Plugin.log("changing pitch of playerID " + playerObj.name);
            Plugin.log("\tpitch: " + modifiedPitch);
            if (SoundManager.Instance == null)
            {
                Plugin.log("SOUNDMANAGER IS NULL", Plugin.LogType.Warning);
                yield break;
            }
            elapsedTime += Time.deltaTime;
            Plugin.log("Elapsed time: " + elapsedTime);
            try
            {
                SoundManager.Instance.SetPlayerPitch(modifiedPitch, (int)playerID); // pr-todo: remove (int)... my VS did weird things with precompiled files...
            }
            catch (NullReferenceException e)
            {
                Plugin.log("Hey...there's a null reference exception in pitch setting....not sure why!", Plugin.LogType.Warning);
                Plugin.log(e.ToString(), Plugin.LogType.Warning);
                Plugin.log(e.StackTrace.ToString(), Plugin.LogType.Warning);
            }
            yield return null; // Wait for the next frame
        }
    }
}
