using GameNetcodeStuff;
using LittleCompany.helper;
using static LittleCompany.events.enemy.EnemyEventManager;

namespace LittleCompany.events.enemy
{
    internal class BeesEventHandler : EnemyEventHandler<RedLocustBees>
    {
        public override void OnDeathShrinking(float previousSize, PlayerControllerB playerShrunkenBy)
        {
            Plugin.Log("Bees shrunken to death");

            Effects.LightningStrikeAtPosition(enemy.transform.position);

            // var bees = (enemy as RedLocustBees);
            base.OnDeathShrinking(previousSize, playerShrunkenBy);
        }

    }
}
