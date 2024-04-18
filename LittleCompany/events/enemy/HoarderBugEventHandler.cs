using GameNetcodeStuff;
using LittleCompany.components;
using LittleCompany.helper;
using LittleCompany.modifications;
using UnityEngine;
using static LittleCompany.events.enemy.EnemyEventManager;

namespace LittleCompany.events.enemy
{
    internal class HoarderBugEventHandler: EnemyEventHandler<HoarderBugAI>
    {
        [DisallowMultipleComponent]
        public class DieingBugBehaviour : MonoBehaviour
        {
            private HoarderBugAI hoarderBug = null;
            public PlayerControllerB GrabbedPlayer = null;

            void Awake()
            {
                hoarderBug = GetComponent<HoarderBugAI>();
            }

            void Update()
            {
                if (hoarderBug?.agent != null)
                    hoarderBug.agent.speed = 10f;

                if(hoarderBug.heldItem != null)
                {
                    if (GrabbedPlayer == null)
                    {
                        // Grabbed something
                        var gpo = hoarderBug.heldItem.itemGrabbableObject as GrabbablePlayerObject;
                        if (gpo != null)
                            GrabbedPlayer = gpo.grabbedPlayer;
                    }

                    if(GrabbedPlayer != null)
                    {
                        GrabbedPlayer.transform.localScale = Vector3.one * EnemyModification.ScalingOf(hoarderBug).RelativeScale;
                    }
                }
            }
        }

        public override void AboutToDeathShrink(float currentSize, PlayerControllerB playerShrunkenBy)
        {
            if (PlayerInfo.IsHost && GrabbablePlayerList.SetPlayerGrabbable(playerShrunkenBy.playerClientId, out GrabbablePlayerObject gpo))
                gpo.HoardingBugTargetUsServerRpc(enemy.NetworkObjectId);

            enemy.gameObject.AddComponent<DieingBugBehaviour>();

            base.AboutToDeathShrink(currentSize, playerShrunkenBy);
        }
        public override void OnDeathShrinking(float previousSize, PlayerControllerB playerShrunkenBy)
        {
            if (enemy.TryGetComponent(out DieingBugBehaviour behaviour))
            {
                behaviour.GrabbedPlayer?.KillPlayer(Vector3.down, false, CauseOfDeath.Unknown);
                Destroy(behaviour);
            }

            base.OnDeathShrinking(previousSize, playerShrunkenBy);
            Plugin.Log("Hoarderbug shrunken to death");
        }
    }
}
