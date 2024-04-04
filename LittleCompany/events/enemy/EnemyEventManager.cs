﻿using System.Collections.Generic;
using System;
using UnityEngine;

using LittleCompany.components;
using LittleCompany.modifications;
using LittleCompany.helper;

namespace LittleCompany.events.enemy
{
    public class EnemyEventManager
    {
        public static readonly Dictionary<string, Type> EventHandler = new Dictionary<string, Type>
        {
            { "Centipede", typeof(CentipedeEventHandler) },
            { "Bunker Spider", typeof(SpiderEventHandler) },
            { "HoarderBug", typeof(HoarderBugEventHandler) },
            { "Flowerman", typeof(BrackenEventHandler) }/*,
            { "Crawler", Enemy.Thumper },
            { "Blob", Enemy.Blob },
            { "DressGirl", Enemy.GhostGirl },
            { "Puffer", Enemy.Puffer },
            { "Nutcracker", Enemy.Nutcracker }*/
        };

        public static Type EventHandlerByName(string enemyName) => EventHandler.GetValueOrDefault(enemyName, typeof(EnemyEventHandler));

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
            internal GameObject deathPoof = null;

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

            public virtual void OnDeathShrinking()
            {
                if (ShrinkRayFX.TryCreateDeathPoofAt(out deathPoof, enemy.transform.position) && enemy.gameObject.TryGetComponent(out AudioSource audioSource) && audioSource != null && Modification.deathPoofSFX != null)
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
