using GameNetcodeStuff;
using static LittleCompany.events.enemy.EnemyEventManager;

namespace LittleCompany.events.enemy
{
    internal class CentipedeEventHandler : EnemyEventHandler
    {
        public override void OnDeathShrinking(float previousSize, PlayerControllerB playerShrunkenBy)
        {
            base.OnDeathShrinking(previousSize, playerShrunkenBy);
            Plugin.Log("Centipede shrunken to death");
        }
    }
}
