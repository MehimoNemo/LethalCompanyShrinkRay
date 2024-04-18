using System.Collections.Generic;
using System;
using UnityEngine;

using LittleCompany.modifications;
using LittleCompany.helper;
using static LittleCompany.helper.EnemyInfo;
using Unity.Netcode;
using System.Collections;
using GameNetcodeStuff;
using static Unity.Netcode.CustomMessagingManager;
using Unity.Collections;

namespace LittleCompany.events.enemy
{
    public class EnemyEventManager
    {
        internal static readonly Dictionary<Enemy, Type> EventHandler = new Dictionary<Enemy, Type>
        {
            { Enemy.Custom,     typeof(DefaultEnemyEventHandler) },
            { Enemy.Centipede,  typeof(CentipedeEventHandler)    },
            { Enemy.Spider,     typeof(SpiderEventHandler)       },
            { Enemy.HoarderBug, typeof(HoarderBugEventHandler)   },
            { Enemy.Bracken,    typeof(BrackenEventHandler)      },
            { Enemy.Slime,      typeof(SlimeEventHandler)        },
            { Enemy.Bees,       typeof(BeesEventHandler)         },
            { Enemy.Coilhead,   typeof(CoilheadEventHandler)     },
            { Enemy.BaboonHawk, typeof(BaboonHawkEventHandler)   },
            { Enemy.Butler,     typeof(ButlerEventHandler)       },
            { Enemy.ButlerBees, typeof(ButlerBeesEventHandler)   },
            { Enemy.Thumper,    typeof(ThumperEventHandler)      },
            { Enemy.Robot,      typeof(RobotEventHandler)        }
        };

        public static Type EventHandlerTypeByName(string enemyName) => EventHandler.GetValueOrDefault(EnemyByName(enemyName), typeof(DefaultEnemyEventHandler));

        public static EnemyEventHandler<T> EventHandlerOf<T>(T enemyAI) where T : EnemyAI
        {
            var eventHandlerType = EventHandlerTypeByName(enemyAI.enemyType.enemyName);
            Plugin.Log("Found eventHandler with name " + eventHandlerType.ToString() + " for enemy name " + enemyAI.enemyType.enemyName);
            if (enemyAI.TryGetComponent(eventHandlerType, out Component eventHandler))
                return eventHandler as EnemyEventHandler<T>;

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

        public class EnemyEventHandler<T> : NetworkBehaviour where T : EnemyAI
        {
            // todo: Make this generic -> public class EnemyEventHandler<T> : MonoBehaviour where T : EnemyAI
            // not working as it can't add the component, as there has to be a type specified...
            internal T enemy = null;
            internal float DeathPoofScale = Effects.DefaultDeathPoofScale;
            private string deathShrinkEventName = null;

            void Awake()
            {
                Plugin.Log(name + " event handler has awaken!");
                enemy = GetComponent<T>();

                deathShrinkEventName = "DeathShrinkEventHandled_" + enemy.enemyType.enemyName;

                if (PlayerInfo.IsHost)
                {
                    Plugin.Log("Current player is the host.");
                    NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(deathShrinkEventName, new HandleNamedMessageDelegate(DeathShrinkEventHandled));
                }

                OnAwake();
#if DEBUG
                //StartCoroutine(SpawnKillLater());
#endif
            }

            public IEnumerator SpawnKillLater()
            {
                yield return new WaitForSeconds(0.5f);
                enemy.transform.localScale = Vector3.zero;
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

            public virtual void AboutToDeathShrink(float currentSize, PlayerControllerB playerShrunkenBy) {
                Plugin.Log("AboutToDeathShrink");
            }

            public virtual void OnDeathShrinking(float previousSize, PlayerControllerB playerShrunkenBy)
            {
                if (Effects.TryCreateDeathPoofAt(out _, enemy.transform.position, DeathPoofScale) && enemy.gameObject.TryGetComponent(out AudioSource audioSource) && audioSource != null && Modification.deathPoofSFX != null)
                    audioSource.PlayOneShot(Modification.deathPoofSFX);

                if (PlayerInfo.IsHost)
                    StartCoroutine(DespawnAfterEventSync());
                else
                    NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(deathShrinkEventName, 0uL, new FastBufferWriter(0, Allocator.Temp), NetworkDelivery.ReliableSequenced);

                Plugin.Log("Enemy shrunken to death");
            }

            #region DeathShrinkSync
            // Has to be done to prevent the enemy from despawning before any client got the OnDeathShrinking event
            private int DeathShrinkSyncedPlayers = 1; // host always got it

            public void DeathShrinkEventHandled(ulong clientId, FastBufferReader reader)
            {
                if (!PlayerInfo.IsHost) // Current player is not the host and therefor not the one who should react
                    return;

                Plugin.Log("DeathShrinkEventHandled");

                DeathShrinkSyncedPlayers++;
            }

            public IEnumerator DespawnAfterEventSync()
            {
                var waitedFrames = 0;
                var playerCount = PlayerInfo.AllPlayers.Count;
                Plugin.Log("EnemyEventManager.PlayerCount: " + playerCount);
                while (waitedFrames < 100 && DeathShrinkSyncedPlayers < playerCount)
                {
                    waitedFrames++;
                    yield return null;
                }

                if (waitedFrames == 100)
                    Plugin.Log("Timeout triggered the death shrink event.", Plugin.LogType.Warning);
                else
                    Plugin.Log("Syncing the death shrink event took " + waitedFrames + " frames.");

                DeathShrinkSyncedPlayers = 1;

                // Now we can despawn it
                enemy.KillEnemyServerRpc(true);
                enemy.enabled = false;
                yield return new WaitForSeconds(3f);
                RoundManager.Instance.DespawnEnemyOnServer(enemy.NetworkObject);
            }
            #endregion

            public virtual void Shrunken(bool wasAlreadyShrunken, PlayerControllerB playerShrunkenBy) { }
            public virtual void Enlarged(bool wasAlreadyEnlarged, PlayerControllerB playerEnlargedBy) { }
            public virtual void ScaledToNormalSize(bool wasShrunken, bool wasEnlarged, PlayerControllerB playerScaledBy) { }
        }
    }
}
