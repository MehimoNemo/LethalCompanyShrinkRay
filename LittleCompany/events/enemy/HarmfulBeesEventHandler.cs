using GameNetcodeStuff;
using LittleCompany.helper;

namespace LittleCompany.events.enemy
{
    internal class HarmfulBeesEventHandler : BeesEventHandler
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
