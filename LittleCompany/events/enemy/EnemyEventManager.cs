using System.Collections.Generic;
using System;
using System.Collections;
using UnityEngine;
using LittleCompany.helper;
using LittleCompany.components;
using static LittleCompany.helper.EnemyInfo;

namespace LittleCompany.events.enemy
{
    public class EnemyEventManager
    {
        internal static readonly Dictionary<Enemy, Type> EventHandler = new Dictionary<Enemy, Type>
        {
            { Enemy.Custom,     typeof(CustomEnemyEventHandler) },
            { Enemy.Centipede,  typeof(CentipedeEventHandler)   },
            { Enemy.Spider,     typeof(SpiderEventHandler)      },
            { Enemy.HoarderBug, typeof(HoarderBugEventHandler)  },
            { Enemy.Bracken,    typeof(BrackenEventHandler)     },
            { Enemy.Slime,      typeof(SlimeEventHandler)       },
            { Enemy.Bees,       typeof(BeesEventHandler)        },
            { Enemy.Coilhead,   typeof(CoilheadEventHandler)    },
            { Enemy.BaboonHawk, typeof(BaboonHawkEventHandler)  },
            { Enemy.Butler,     typeof(ButlerEventHandler)      },
            { Enemy.ButlerBees, typeof(ButlerBeesEventHandler)  },
            { Enemy.Thumper,    typeof(ThumperEventHandler)     },
            { Enemy.Robot,      typeof(RobotEventHandler)       },
            { Enemy.Worm,       typeof(WormEventHandler)        }
        };

        public static Type EventHandlerTypeByName(string enemyName) => EventHandler.GetValueOrDefault(EnemyByName(enemyName), typeof(CustomEnemyEventHandler));

        public static EnemyEventHandler EventHandlerOf(EnemyAI enemyAI)
        {
            var eventHandlerType = EventHandlerTypeByName(enemyAI.enemyType.enemyName);
            Plugin.Log("Found eventHandler with name " + eventHandlerType.ToString() + " for enemy name " + enemyAI.enemyType.enemyName);
            if (enemyAI.TryGetComponent(eventHandlerType, out Component eventHandler))
                return eventHandler as EnemyEventHandler;

            Plugin.Log("Enemy had no event handler!", Plugin.LogType.Error);
            return null;
        }

        public static void BindAllEventHandler()
        {
            int handlersAdded = 0;
            foreach (var enemyType in EnemyTypes)
            {
                if (enemyType.enemyPrefab == null)
                {
                    Plugin.Log("Enemy " + enemyType.enemyName + " had no enemyPrefab. Unable to connect enemy event handler.", Plugin.LogType.Warning);
                    continue;
                }

                var eventHandlerType = EventHandlerTypeByName(enemyType.enemyName);
                if(enemyType.enemyPrefab != null)
                {
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
            }

            Plugin.Log("BindAllEnemyEvents -> Added handler for " + handlersAdded + "/" + EnemyTypes.Count + " enemies.");
        }

        public static void LoadEventPrefabs()
        {
            BrackenEventHandler.LoadBrackenOrbPrefab();
            RobotEventHandler.LoadBurningRobotToyPrefab();
        }

        public class EnemyEventHandler : EventHandlerBase
        {
            internal EnemyAI enemy = null;

            public override void OnAwake()
            {
                enemy = GetComponent<EnemyAI>();
                GetComponent<EnemyScaling>()?.AddListener(this);
#if DEBUG
                //StartCoroutine(SpawnKillLater());
#endif
            }

#if DEBUG
            private IEnumerator SpawnKillLater()
            {
                yield return new WaitForSeconds(0.5f);
                enemy.transform.localScale = Vector3.zero;
                OnDeathShrinking(1f, PlayerInfo.ControllerFromID(0ul)); // SPAWNKILL !!
            }
#endif

            public override void DestroyObject()
            {
                enemy.KillEnemyServerRpc(true);
                enemy.enabled = false;
            }

            public override void DespawnObject()
            {
                RoundManager.Instance.DespawnEnemyOnServer(enemy.NetworkObject);
            }
        }
    }
}
