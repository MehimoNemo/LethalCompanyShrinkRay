﻿using GameNetcodeStuff;
using static LittleCompany.events.enemy.EnemyEventManager;

namespace LittleCompany.events.enemy
{
    internal class BrackenEventHandler : EnemyEventHandler
    {
        public override void OnDeathShrinking(float previousSize, PlayerControllerB playerShrunkenBy)
        {
            base.OnDeathShrinking(previousSize, playerShrunkenBy);
            Plugin.Log("Bracken shrunken to death");
        }
        public override void Shrunken(bool wasShrunkenBefore, PlayerControllerB playerShrunkenBy) { }
        public override void Enlarged(bool wasEnlargedBefore, PlayerControllerB playerEnlargedBy) { }
        public override void ScaledToNormalSize(bool wasShrunken, bool wasEnlarged, PlayerControllerB playerScaledBy) { }
    }
}
