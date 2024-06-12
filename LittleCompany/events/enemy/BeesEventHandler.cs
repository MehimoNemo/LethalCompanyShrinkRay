using GameNetcodeStuff;
using LittleCompany.helper;
using UnityEngine;
using static LittleCompany.events.enemy.EnemyEventManager;

namespace LittleCompany.events.enemy
{
    internal class BeesEventHandler : EnemyEventHandler
    {
        public override void OnAwake()
        {
            base.OnAwake();

            foreach (var particles in enemy.GetComponentsInChildren<ParticleSystem>())
            {
                var main = particles.main;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            }
        }
    }
}
