using GameNetcodeStuff;
using static LittleCompany.events.enemy.EnemyEventManager;

namespace LittleCompany.events.enemy
{
    internal class ButlerBeesEventHandler : EnemyEventHandler<ButlerBeesEnemyAI>
    {
        public override void OnDeathShrinking(float previousSize, PlayerControllerB playerShrunkenBy)
        {
            Plugin.Log("Butler bees shrunken to death");

            base.OnDeathShrinking(previousSize, playerShrunkenBy);
        }
    }
}
