using GameNetcodeStuff;
using LethalLib.Modules;
using LittleCompany.components;
using LittleCompany.helper;
using LittleCompany.modifications;
using System.Drawing;
using UnityEngine;
using static LittleCompany.events.enemy.EnemyEventManager;

namespace LittleCompany.events.enemy
{
    internal class HoarderBugEventHandler: EnemyEventHandler
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

                if(hoarderBug.targetItem != null)
                {
                    if (GrabbedPlayer == null)
                    {
                        // Grabbed something
                        var gpo = hoarderBug.targetItem as GrabbablePlayerObject;
                        if (gpo != null)
                        {
                            GrabbedPlayer = gpo.grabbedPlayer;
                            Plugin.Log(gpo.grabbedPlayer.name + " got grabbed by a dieing hoarding bug");
                        }
                    }

                    if(GrabbedPlayer != null)
                    {
                        GrabbedPlayer.transform.localScale = Vector3.one * EnemyModification.ScalingOf(hoarderBug).RelativeScale;
                        
                        // Scaling with hoarder bug
                        /*var relativeBugScale = new Vector3()
                        {
                            x = 1f / bugScaleOnGrab.x * hoarderBug.transform.localScale.x,
                            y = 1f / bugScaleOnGrab.y * hoarderBug.transform.localScale.y,
                            z = 1f / bugScaleOnGrab.z * hoarderBug.transform.localScale.z
                        };
                        Plugin.Log("Multiplier: " + relativeBugScale);

                        var player = PlayerInfo.ControllerFromID(grabbedPlayerID.Value);
                        player.transform.localScale = new Vector3()
                        {
                            x = targetItemGrabScale.x * relativeBugScale.x,
                            y = targetItemGrabScale.y * relativeBugScale.y,
                            z = targetItemGrabScale.z * relativeBugScale.z,
                        };*/
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
            if(enemy.TryGetComponent(out DieingBugBehaviour behaviour))
                    behaviour.GrabbedPlayer?.KillPlayer(Vector3.down, false, CauseOfDeath.Unknown);

            base.OnDeathShrinking(previousSize, playerShrunkenBy);
            Plugin.Log("Hoarderbug shrunken to death");
        }
        public override void Shrunken(bool wasShrunkenBefore, PlayerControllerB playerShrunkenBy) { }
        public override void Enlarged(bool wasEnlargedBefore, PlayerControllerB playerEnlargedBy) { }
        public override void ScaledToNormalSize(bool wasShrunken, bool wasEnlarged, PlayerControllerB playerScaledBy) { }
    }
}
