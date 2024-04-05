using System.Collections.Generic;
using System;
using UnityEngine;

using LittleCompany.components;
using LittleCompany.modifications;
using LittleCompany.helper;
using static LittleCompany.helper.EnemyInfo;

namespace LittleCompany.events.enemy
{
    public class EnemyEventManager
    {
        internal static readonly Dictionary<Enemy, Type> EventHandler = new Dictionary<Enemy, Type>
        {
            { Enemy.Centipede,  typeof(CentipedeEventHandler)   },
            { Enemy.Spider,     typeof(SpiderEventHandler)      },
            { Enemy.HoarderBug, typeof(HoarderBugEventHandler)  },
            { Enemy.Bracken,    typeof(BrackenEventHandler)     }
        };

        public static Type EventHandlerByName(string enemyName) => EventHandler.GetValueOrDefault(EnemyByName(enemyName), typeof(EnemyEventHandler));

        public static EnemyEventHandler EventHandlerOf(EnemyAI enemyAI)
        {
            var eventHandlerType = EventHandlerByName(enemyAI.enemyType.enemyName);
            Plugin.Log("Found eventHandler with name " + eventHandlerType.ToString() + " for enemy name " + enemyAI.enemyType.enemyName);
            if (enemyAI.TryGetComponent(eventHandlerType, out Component eventHandler))
                return eventHandler as EnemyEventHandler;

            return enemyAI.gameObject.AddComponent(eventHandlerType) as EnemyEventHandler;
        }

        public class EnemyEventHandler : MonoBehaviour
        {
            // todo: Make this generic -> public class EnemyEventHandler<T> : MonoBehaviour where T : EnemyAI
            // not working as it can't add the component, as there has to be a type specified...
            internal EnemyAI enemy = null;
            internal float DeathPoofScale = ShrinkRayFX.DefaultDeathPoofScale;

            void Awake()
            {
                enemy = GetComponent<EnemyAI>();
            }

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
