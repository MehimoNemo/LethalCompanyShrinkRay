using LittleCompany.components;
using System;
using System.Collections.Generic;
using UnityEngine;

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
        }

        public static Dictionary<string, Enemy> EnemyNameMap = new Dictionary<string, Enemy>()
        {
            { "Centipede",          Enemy.Centipede     },
            { "Bunker Spider",      Enemy.Spider        },
            { "Hoarding bug",       Enemy.HoarderBug    },
            { "Flowerman",          Enemy.Bracken       },
            { "Crawler",            Enemy.Thumper       },
            { "Blob",               Enemy.Slime         },
            { "Girl",               Enemy.GhostGirl     },
            { "Puffer",             Enemy.Puffer        },
            { "Nutcracker",         Enemy.Nutcracker    },
            { "MouthDog",           Enemy.EyelessDog    },
            { "ForestGiant",        Enemy.ForestGiant   },
            { "Earth Leviathan",    Enemy.Worm          },
            { "Red Locust Bees",    Enemy.Bees          },
            { "Manticoil",          Enemy.ManticoilBird },
            { "Docile Locust Bees", Enemy.HarmlessBees  },
            { "Baboon hawk",        Enemy.BaboonHawk    },
            { "Spring",             Enemy.Coilhead      },
            { "Jester",             Enemy.Jester        },
            { "Lasso",              Enemy.LassoMan      },
            { "Masked",             Enemy.Masked        }
        };

        public static Enemy EnemyByName(string name) => EnemyNameMap.GetValueOrDefault(name, Enemy.Custom);

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

        public static List<String> AllEnemyNames
        {
            get
            {
                var list = new List<string>();
                foreach (var enemyType in UnityEngine.Object.FindObjectsOfType<EnemyType>())
                    list.Add(enemyType.enemyName);
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
