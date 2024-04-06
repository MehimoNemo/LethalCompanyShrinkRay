﻿using LittleCompany.components;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static LittleCompany.components.GrabbablePlayerObject;

namespace LittleCompany.helper
{
    public class EnemyInfo
    {
        public enum Enemy
        {
            Custom = 0,

            BaboonHawk,
            Bees,
            BeesHarmless,
            Bracken,
            Butler,
            Centipede,
            Coilhead,
            EyelessDog,
            ForestGiant,
            GhostGirl,
            HoarderBug,
            Jester,
            LassoMan,
            ManticoilBird,
            Masked,
            Nutcracker,
            Puffer,
            Robot,
            Slime,
            Spider,
            Thumper,
            Worm
        }

        public static Dictionary<string, Enemy> EnemyNameMap = new Dictionary<string, Enemy>()
        {
            { "Custom",             Enemy.Custom        },

            { "Baboon hawk",        Enemy.BaboonHawk    },
            { "Red Locust Bees",    Enemy.Bees          },
            { "Docile Locust Bees", Enemy.BeesHarmless  },
            { "Flowerman",          Enemy.Bracken       },
            { "Butler",             Enemy.Butler        },
            { "Centipede",          Enemy.Centipede     },
            { "Spring",             Enemy.Coilhead      },
            { "MouthDog",           Enemy.EyelessDog    },
            { "ForestGiant",        Enemy.ForestGiant   },
            { "Girl",               Enemy.GhostGirl     },
            { "Hoarding bBug",      Enemy.HoarderBug    },
            { "Jester",             Enemy.Jester        },
            { "Lasso",              Enemy.LassoMan      },
            { "Manticoil",          Enemy.ManticoilBird },
            { "Masked",             Enemy.Masked        },
            { "Nutcracker",         Enemy.Nutcracker    },
            { "Puffer",             Enemy.Puffer        },
            { "RadMech",            Enemy.Robot         },
            { "Blob",               Enemy.Slime         },
            { "Bunker Spider",      Enemy.Spider        },
            { "Crawler",            Enemy.Thumper       },
            { "Earth Leviathan",    Enemy.Worm          },
        };

        public static Enemy EnemyByName(string name) => EnemyNameMap.GetValueOrDefault(name, Enemy.Custom);

        public static string EnemyNameOf(Enemy enemy) => EnemyNameMap.FirstOrDefault((x) => x.Value == enemy).Key;

        public static EnemyType EnemyTypeByName(string enemyName = null)
        {
            if (enemyName == null || RoundManager.Instance?.currentLevel == null) return null;

            // todo: optimize and store in list that gets updated on level change
            var enemyList = new List<SpawnableEnemyWithRarity>();
            enemyList.AddRange(RoundManager.Instance.currentLevel.Enemies);
            enemyList.AddRange(RoundManager.Instance.currentLevel.OutsideEnemies);
            enemyList.AddRange(RoundManager.Instance.currentLevel.DaytimeEnemies);

            var index = enemyList.FindIndex(spawnableEnemy => spawnableEnemy.enemyType.enemyName == enemyName);
            if (index == -1) return null;

            return enemyList[index].enemyType;
        }

        public static EnemyAI SpawnEnemyAt(Vector3 spawnPosition, float yRot, EnemyType enemyType)
        {
            GameObject gameObject = UnityEngine.Object.Instantiate(enemyType.enemyPrefab, spawnPosition, Quaternion.Euler(new Vector3(0f, yRot, 0f)));
            gameObject.GetComponentInChildren<NetworkObject>().Spawn(destroyWithScene: true);
            var enemyAI = gameObject.GetComponent<EnemyAI>();
            RoundManager.Instance.SpawnedEnemies.Add(enemyAI);
            return enemyAI;
        }

        public static Enemy RandomEnemy
        {
            get
            {
                var enemies = Enum.GetValues(typeof(Enemy));
                return (Enemy) enemies.GetValue(UnityEngine.Random.Range(1, enemies.Length));
            }
        }

        public static float LargestGrabbingEnemy
        {
            get
            {
                var scale = 1f;
                foreach(var enemyScaling in UnityEngine.Object.FindObjectsOfType<EnemyScaling>())
                {
                    if(enemyScaling.target is HoarderBugAI)
                        scale = Mathf.Max(scale, enemyScaling.RelativeScale);
                }
                return scale;
            }
        }

        public static List<string> SpawnedEnemyNames => RoundManager.Instance.SpawnedEnemies.Select(enemyType => enemyType.enemyType.enemyName).ToList();

        public static List<string> CurrentLevelEnemyNames
        {
            get
            {
                if (RoundManager.Instance?.currentLevel == null)
                    return null;

                var list = new List<string>();
                RoundManager.Instance.currentLevel.Enemies.ForEach(enemyType => list.Add(enemyType.enemyType.enemyName));
                RoundManager.Instance.currentLevel.OutsideEnemies.ForEach(enemyType => list.Add(enemyType.enemyType.enemyName));
                RoundManager.Instance.currentLevel.DaytimeEnemies.ForEach(enemyType => list.Add(enemyType.enemyType.enemyName));
                return list;
            }
        }

        public static float SizeOf(EnemyAI enemyAI) => enemyAI.TryGetComponent(out EnemyScaling scaling) ? scaling.RelativeScale : 1f;

        internal class HoarderBug
        {
            internal enum BehaviourState
            {
                Searching = 0,
                Nest,
                Chase
            }
        }
    }
}