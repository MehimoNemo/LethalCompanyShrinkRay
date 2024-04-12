using GameNetcodeStuff;
using static LittleCompany.events.enemy.EnemyEventManager;

namespace LittleCompany.events.enemy
{
    internal class ButlerBeesEventHandler : EnemyEventHandler
    {
        public override void OnDeathShrinking(float previousSize, PlayerControllerB playerShrunkenBy)
        {
            Plugin.Log("Butler bees shrunken to death");

            var butlerBees = (enemy as ButlerBeesEnemyAI);
            butlerBees.EnableEnemyMesh(false);

            base.OnDeathShrinking(previousSize, playerShrunkenBy);
        }
    }
}
