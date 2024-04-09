using System.Collections.Generic;
using System;
using UnityEngine;

using LittleCompany.components;
using LittleCompany.modifications;
using LittleCompany.helper;
using static LittleCompany.helper.EnemyInfo;
using Unity.Netcode;
using System.Collections;

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
            { Enemy.Coilhead,   typeof(CoilheadEventHandler)    }
        };

        public static Type EventHandlerTypeByName(string enemyName) => EventHandler.GetValueOrDefault(EnemyByName(enemyName), typeof(EnemyEventHandler));

        public static EnemyEventHandler EventHandlerOf(EnemyAI enemyAI)
        {
            if (!PlayerInfo.IsHost) return null; // todo: optimize

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

        public class EnemyEventHandler : NetworkBehaviour
        {
            // todo: Make this generic -> public class EnemyEventHandler<T> : MonoBehaviour where T : EnemyAI
            // not working as it can't add the component, as there has to be a type specified...
            internal EnemyAI enemy = null;
            internal float DeathPoofScale = ShrinkRayFX.DefaultDeathPoofScale;

            void Awake()
            {
                Plugin.Log("EnemyEventHandler " + name + " has awaken!");
                enemy = GetComponent<EnemyAI>();
            }

            void Start()
            {
                Plugin.Log("EnemyEventHandler " + name + " has started!");
            }

            public override void OnNetworkSpawn()
            {
                base.OnNetworkSpawn();
                Plugin.Log("EnemyEventHandler " + name + " got spawned!");

#if DEBUG
                if (PlayerInfo.IsHost)
                    StartCoroutine(SpawnKillLater());
#endif
            }

            public IEnumerator SpawnKillLater()
            {
                yield return new WaitForSeconds(1);
                OnDeathShrinking(1f); // SPAWNKILL !!
            }

            public virtual void OnAwake() { }

            public void SizeChanged(float from, float to)
            {
                if (Mathf.Approximately(from, to)) return;
                if (from > to)
                    Shrunken(from <= 1f);
                else
                    Enlarged(from >= 1f);
            }

            public virtual void OnDeathShrinking(float previousSize)
            {
                if (ShrinkRayFX.TryCreateDeathPoofAt(out _, enemy.transform.position, DeathPoofScale) && enemy.gameObject.TryGetComponent(out AudioSource audioSource) && audioSource != null && Modification.deathPoofSFX != null)
                    audioSource.PlayOneShot(Modification.deathPoofSFX);

                if (PlayerInfo.IsHost)
                    RoundManager.Instance.DespawnEnemyOnServer(enemy.NetworkObject);

                Plugin.Log("Enemy shrunken to death");
            }

            public virtual void Shrunken(bool wasAlreadyShrunken) { }
            public virtual void Enlarged(bool wasAlreadyEnlarged) { }
            public virtual void ScaledToNormalSize(bool wasShrunken, bool wasEnlarged) { }
        }
    }
}
