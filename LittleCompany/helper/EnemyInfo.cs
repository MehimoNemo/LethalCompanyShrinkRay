using LittleCompany.components;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static LittleCompany.components.GrabbablePlayerObject;

namespace LittleCompany.helper
{
    public class EnemyInfo
    {
        public enum Enemy
        {
            Custom = 0,
            Centipede,
            Spider,
            HoarderBug,
            Bracken,
            Thumper,
            Slime,
            GhostGirl,
            Puffer,
            Nutcracker,
            EyelessDog,
            ForestGiant,
            Worm,
            Bees,
            ManticoilBird,
            HarmlessBees,
            BaboonHawk,
            Coilhead,
            Jester,
            LassoMan,
            Masked,
            Butler,
            Robot
        }

        public static Dictionary<string, Enemy> EnemyNameMap = new Dictionary<string, Enemy>()
        {
            { "Centipede",          Enemy.Centipede     },
            { "SandSpider",         Enemy.Spider        },
            { "HoarderBug",         Enemy.HoarderBug    },
            { "Flowerman",          Enemy.Bracken       },
            { "Crawler",            Enemy.Thumper       },
            { "Blob",               Enemy.Slime         },
            { "DressGirl",          Enemy.GhostGirl     },
            { "Puffer",             Enemy.Puffer        },
            { "Nutcracker",         Enemy.Nutcracker    },
            { "MouthDog",           Enemy.EyelessDog    },
            { "ForestGiant",        Enemy.ForestGiant   },
            { "SandWorm",           Enemy.Worm          },
            { "RedLocustBees",      Enemy.Bees          },
            { "Doublewing",         Enemy.ManticoilBird },
            { "DocileLocustBees",   Enemy.HarmlessBees  },
            { "BaboonHawk",         Enemy.BaboonHawk    },
            { "SpringMan",          Enemy.Coilhead      },
            { "Jester",             Enemy.Jester        },
            { "LassoMan",           Enemy.LassoMan      },
            { "MaskedPlayerEnemy",  Enemy.Masked        },
            { "Butler",             Enemy.Butler        },
            { "RadMech",            Enemy.Robot         }
        };

        public static Enemy EnemyByName(string name) => EnemyNameMap.GetValueOrDefault(name, Enemy.Custom);

        public static string EnemyNameOf(Enemy enemy) => EnemyNameMap.FirstOrDefault((x) => x.Value == enemy).Key;

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

        public static List<string> SpawnedEnemyNames => RoundManager.Instance.SpawnedEnemies.Select(enemyType => enemyType.enemyType.name).ToList();

        public static List<string> CurrentLevelEnemyNames
        {
            get
            {
                if (RoundManager.Instance?.currentLevel == null)
                    return null;

                var list = new List<string>();
                RoundManager.Instance.currentLevel.Enemies.ForEach(enemyType => list.Add(enemyType.enemyType.name));
                RoundManager.Instance.currentLevel.OutsideEnemies.ForEach(enemyType => list.Add(enemyType.enemyType.name));
                RoundManager.Instance.currentLevel.DaytimeEnemies.ForEach(enemyType => list.Add(enemyType.enemyType.name));
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
