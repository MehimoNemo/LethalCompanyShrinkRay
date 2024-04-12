using System.Collections.Generic;
using System;
using UnityEngine;

using LittleCompany.components;
using LittleCompany.modifications;
using LittleCompany.helper;
using static LittleCompany.helper.EnemyInfo;
using Unity.Netcode;
using System.Collections;
using GameNetcodeStuff;

namespace LittleCompany.events.enemy
{
    public class EnemyEventManager
    {
        internal static readonly Dictionary<Enemy, Type> EventHandler = new Dictionary<Enemy, Type>
        {
            { Enemy.Centipede,  typeof(CentipedeEventHandler)   },
            { Enemy.Spider,     typeof(SpiderEventHandler)      },
            { Enemy.HoarderBug, typeof(HoarderBugEventHandler)  },
            { Enemy.Bracken,    typeof(BrackenEventHandler)     },
            { Enemy.Slime,      typeof(SlimeEventHandler)       },
            { Enemy.Bees,       typeof(BeesEventHandler)        },
            { Enemy.Coilhead,   typeof(CoilheadEventHandler)    },
            { Enemy.BaboonHawk, typeof(BaboonHawkEventHandler)  },
            { Enemy.Butler,     typeof(ButlerEventHandler)      },
            { Enemy.Robot,      typeof(RobotEventHandler)       }
        };

        public static Type EventHandlerTypeByName(string enemyName) => EventHandler.GetValueOrDefault(EnemyByName(enemyName), typeof(EnemyEventHandler));

        public static EnemyEventHandler EventHandlerOf(EnemyAI enemyAI)
        {
            var eventHandlerType = EventHandlerTypeByName(enemyAI.enemyType.enemyName);
            Plugin.Log("Found eventHandler with name " + eventHandlerType.ToString() + " for enemy name " + enemyAI.enemyType.enemyName);
            if (enemyAI.TryGetComponent(eventHandlerType, out Component eventHandler))
                return eventHandler as EnemyEventHandler;

            Plugin.Log("Enemy had no event handler!", Plugin.LogType.Error);
            return null;
        }

        public static void BindAllEnemyEvents()
        {
            int handlersAdded = 0;
            foreach (var enemyType in EnemyTypes)
            {
                var eventHandlerType = EventHandlerTypeByName(enemyType.enemyName);
                var eventHandler = enemyType.enemyPrefab.AddComponent(eventHandlerType);
                if (eventHandler != null)
                    handlersAdded++;
#if DEBUG
                if (eventHandler != null)
                    Plugin.Log("Added event handler \"" + eventHandlerType.Name + "\" for enemy \"" + enemyType.enemyName + "\"");
                else
                    Plugin.Log("No enemy handler found for enemy \"" + enemyType.enemyName + "\"");
#endif
            }

            Plugin.Log("BindAllEnemyEvents -> Added handler for " + handlersAdded + "/" + EnemyTypes.Count + " enemies.");
        }

        public static void LoadEventPrefabs()
        {
            BrackenEventHandler.LoadBrackenOrbPrefab();
            RobotEventHandler.LoadBurningRobotToyPrefab();
        }

        public class EnemyEventHandler : NetworkBehaviour
        {
            // todo: Make this generic -> public class EnemyEventHandler<T> : MonoBehaviour where T : EnemyAI
            // not working as it can't add the component, as there has to be a type specified...
            internal EnemyAI enemy = null;
            internal float DeathPoofScale = ShrinkRayFX.DefaultDeathPoofScale;

            void Awake()
            {
                Plugin.Log(name + " event handler has awaken!");
                enemy = GetComponent<EnemyAI>();

                OnAwake();
#if DEBUG
                StartCoroutine(SpawnKillLater());
#endif
            }

            public IEnumerator SpawnKillLater()
            {
                yield return new WaitForSeconds(0.5f);
                OnDeathShrinking(1f, PlayerInfo.ControllerFromID(0ul)); // SPAWNKILL !!
            }

            public virtual void OnAwake() { }

            public void SizeChanged(float from, float to, PlayerControllerB playerBy)
            {
                if (Mathf.Approximately(from, to)) return;
                if (from > to)
                    Shrunken(from <= 1f, playerBy);
                else
                    Enlarged(from >= 1f, playerBy);
            }

            public virtual void AboutToDeathShrink(float currentSize, PlayerControllerB playerShrunkenBy) { }

            public virtual void OnDeathShrinking(float previousSize, PlayerControllerB playerShrunkenBy)
            {
                if (ShrinkRayFX.TryCreateDeathPoofAt(out _, enemy.transform.position, DeathPoofScale) && enemy.gameObject.TryGetComponent(out AudioSource audioSource) && audioSource != null && Modification.deathPoofSFX != null)
                    audioSource.PlayOneShot(Modification.deathPoofSFX);

                if (PlayerInfo.IsHost)
                    StartCoroutine(DespawnAfterEventSync());
                else
                    DeathShrinkEventReceivedServerRpc();

                Plugin.Log("Enemy shrunken to death");
            }

            #region DeathShrinkSync
            // Has to be done to prevent the enemy from despawning before any client got the OnDeathShrinking event
            private int DeathShrinkSyncedPlayers = 1; // host always got it
            public IEnumerator DespawnAfterEventSync()
            {
                var waitedFrames = 0;
                var playerCount = PlayerInfo.AllPlayers.Count;
                while (waitedFrames < 100 && DeathShrinkSyncedPlayers < playerCount)
                {
                    waitedFrames++;
                    yield return null;
                }

                if (waitedFrames == 100)
                    Plugin.Log("Timeout triggered the death shrink event.", Plugin.LogType.Warning);
                else
                    Plugin.Log("Syncing the death shrink event took " + waitedFrames + " frames.", Plugin.LogType.Warning);

                // Now we can despawn it
                RoundManager.Instance.DespawnEnemyOnServer(enemy.NetworkObject);
                DeathShrinkSyncedPlayers = 1;
            }

            [ServerRpc(RequireOwnership = false)]
            public void DeathShrinkEventReceivedServerRpc()
            {
                DeathShrinkSyncedPlayers++;
            }
            #endregion

            public virtual void Shrunken(bool wasAlreadyShrunken, PlayerControllerB playerShrunkenBy) { }
            public virtual void Enlarged(bool wasAlreadyEnlarged, PlayerControllerB playerEnlargedBy) { }
            public virtual void ScaledToNormalSize(bool wasShrunken, bool wasEnlarged, PlayerControllerB playerScaledBy) { }
        }
    }
}
