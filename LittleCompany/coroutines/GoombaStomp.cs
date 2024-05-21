using GameNetcodeStuff;
using LittleCompany.helper;
using LittleCompany.modifications;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static LittleCompany.components.GrabbablePlayerObject;

namespace LittleCompany.coroutines
{
    internal class GoombaStomp
    {
        public struct GoombaRoutine
        {
            public Coroutine coroutine = null;
            public float initialSize = 1f;
            public Action onComplete = null;

            public GoombaRoutine(Coroutine coroutine, float initialSize, Action onComplete)
            {
                this.coroutine = coroutine;
                this.initialSize = initialSize;
                this.onComplete = onComplete;
            }
        }

        public static Dictionary<PlayerControllerB, GoombaRoutine> currentlyGoombadPlayers = new Dictionary<PlayerControllerB, GoombaRoutine>();

        public static void GoombaPlayer(PlayerControllerB player, Action onComplete = null)
        {
            if (player == null) return;

            if (IsGettingGoombad(player))
            {
                Plugin.Log("Player already getting goombad.");
                return;
            }

            var initialSize = PlayerInfo.SizeOf(player);
            var coroutine = player.StartCoroutine(GoombaCoroutine(player, () =>
            {
                currentlyGoombadPlayers.Remove(player);
                if (onComplete != null)
                    onComplete();
            }));

            if(coroutine != null)
                currentlyGoombadPlayers.Add(player, new GoombaRoutine(coroutine, initialSize, onComplete));
        }

        public static bool IsGettingGoombad(PlayerControllerB player)
        {
            return currentlyGoombadPlayers.ContainsKey(player);
        }

        public static void StopGoombaOn(PlayerControllerB player)
        {
            if (!IsGettingGoombad(player)) return;

            var goombaRoutine = currentlyGoombadPlayers[player];
            player.StopCoroutine(goombaRoutine.coroutine);
            player.transform.localScale = Vector3.one * goombaRoutine.initialSize;
            if(goombaRoutine.onComplete != null)
                goombaRoutine.onComplete();

            currentlyGoombadPlayers.Remove(player);
        }

        private static IEnumerator GoombaCoroutine(PlayerControllerB targetPlayer, Action onComplete)
        {
            Plugin.Log("Starting goomba coroutine");
            var initialSize = PlayerInfo.SizeOf(targetPlayer);

            AnimationCurve scaleCurve = new AnimationCurve(
                new Keyframe(0, initialSize),
                new Keyframe(0.05f, initialSize / 10f),
                new Keyframe(0.85f, initialSize/ 5f),
                new Keyframe(1f, initialSize)
            );
            scaleCurve.preWrapMode = WrapMode.PingPong;
            scaleCurve.postWrapMode = WrapMode.PingPong;

            AnimationCurve stretchCurve = new AnimationCurve(
                new Keyframe(0, initialSize * 2f),
                new Keyframe(0.5f, initialSize * 1.5f),
                new Keyframe(1f, initialSize)
            );

            float duration = 5f;
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                float scaleValue = scaleCurve.Evaluate(elapsedTime / duration);
                float stretchValue = stretchCurve.Evaluate(elapsedTime / duration);

                targetPlayer.transform.localScale = new Vector3(stretchValue, scaleValue, stretchValue);

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            onComplete();
        }
    }
}
