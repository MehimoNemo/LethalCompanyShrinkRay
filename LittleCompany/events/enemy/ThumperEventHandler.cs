using GameNetcodeStuff;
using HarmonyLib;
using LittleCompany.helper;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using static LittleCompany.events.enemy.EnemyEventManager;

namespace LittleCompany.events.enemy
{
    internal class ThumperEventHandler : EnemyEventHandler<CrawlerAI>
    {
        internal class ResistantQuicksandTrigger : MonoBehaviour
        {
            public static bool sinkingLocalPlayer = false;
            public int audioClipIndex = 0;

            public float movementHinderance = 1.6f;

            public float sinkingSpeedMultiplier = 0.15f;

            void Awake()
            {
                name = "ResistantQuicksand";
            }

            private void OnTriggerStay(Collider other)
            {
                if(!other.gameObject.TryGetComponent(out PlayerControllerB player) || player != PlayerInfo.CurrentPlayer)
                    return;

                player.statusEffectAudioIndex = audioClipIndex;
                if (player.isSinking || sinkingLocalPlayer)
                    return;

                Plugin.Log("ResistantQuicksand: Sinking");

                sinkingLocalPlayer = true;
                player.sourcesCausingSinking++;
                player.isMovementHindered++;
                player.hinderedMultiplier *= movementHinderance;
                player.sinkingSpeedMultiplier = sinkingSpeedMultiplier;
            }

            private void OnTriggerExit(Collider other)
            {
                if (!sinkingLocalPlayer)
                    return;

                if (!other.CompareTag("Player") || !other.gameObject.TryGetComponent(out PlayerControllerB player))
                    return;

                if (player != PlayerInfo.CurrentPlayer)
                    return;

                Plugin.Log("ResistantQuicksand: Not sinking");

                sinkingLocalPlayer = false;
                player.sourcesCausingSinking = Mathf.Clamp(player.sourcesCausingSinking - 1, 0, 100);
                player.isMovementHindered = Mathf.Clamp(player.isMovementHindered - 1, 0, 100);
                player.hinderedMultiplier = Mathf.Clamp(player.hinderedMultiplier / movementHinderance, 1f, 100f);
            }
        }

        public override void OnAwake()
        {
            DeathPoofScale = 3f;
            base.OnAwake();
        }

        public override void AboutToDeathShrink(float currentSize, PlayerControllerB playerShrunkenBy)
        {
            // todo: Screaming sound
            int num = Random.Range(0, enemy.longRoarSFX.Length);
            enemy.creatureVoice.PlayOneShot(enemy.longRoarSFX[num]);

            base.AboutToDeathShrink(currentSize, playerShrunkenBy);
        }

        public override void OnDeathShrinking(float previousSize, PlayerControllerB playerShrunkenBy)
        {
            Plugin.Log("Thumper shrunken to death");

            // todo: thumping sound (enemy.hitWallSFX)

            GameObject quicksand = Instantiate(RoundManager.Instance.quicksandPrefab, enemy.transform.position + Vector3.up, Quaternion.identity, RoundManager.Instance.mapPropsContainer.transform);

            if (!IsQuicksandableSurfaceBelow(enemy.transform.position))
            {
                // Have to adjust it ..
                var quicksandProjector = quicksand.GetComponentInChildren<DecalProjector>();
                if (quicksandProjector != null)
                {
                    quicksandProjector.material.color = new Color(0.5f, 0.2f, 0.2f); // QuicksandTex
                    quicksandProjector.decalLayerMask = DecalLayerEnum.Everything;
                    quicksandProjector.drawDistance = 1f;
                    quicksandProjector.endAngleFade = 1f;
                    quicksandProjector.fadeFactor = 1f;
                    quicksandProjector.fadeScale = 1f;
                }
            }

            var trigger = quicksand.GetComponentInChildren<QuicksandTrigger>();
            if (trigger != null)
            {
                trigger.gameObject.AddComponent<ResistantQuicksandTrigger>();
                Destroy(trigger);
            }

            base.OnDeathShrinking(previousSize, playerShrunkenBy);
        }

        public bool IsQuicksandableSurfaceBelow(Vector3 position) // PlayerControllerB.CheckConditionsForSinkingInQuicksand
        {
            var interactRay = new Ray(position + Vector3.up, -Vector3.up);
            if (Physics.Raycast(interactRay, out RaycastHit hit, 6f, StartOfRound.Instance.walkableSurfacesMask, QueryTriggerInteraction.Ignore))
            {
                for (int i = 0; i < StartOfRound.Instance.footstepSurfaces.Length; i++)
                {
                    if (hit.collider.CompareTag(StartOfRound.Instance.footstepSurfaces[i].surfaceTag))
                        return (i == 1 || i == 4 || i == 8);
                }
            }

            return false;
        }

        #region Patches
        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.CheckConditionsForSinkingInQuicksand))]
        [HarmonyPostfix]
        public static bool CheckConditionsForSinkingInQuicksand(bool __result, PlayerControllerB __instance)
        {
            if (ResistantQuicksandTrigger.sinkingLocalPlayer && __instance == PlayerInfo.CurrentPlayer)
                return true;

            return __result;
        }
        #endregion
    }
}
