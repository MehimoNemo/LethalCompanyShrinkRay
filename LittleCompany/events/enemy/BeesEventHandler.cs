﻿using GameNetcodeStuff;
using LittleCompany.helper;
using System.Collections;
using UnityEngine;
using static LittleCompany.events.enemy.EnemyEventManager;

namespace LittleCompany.events.enemy
{
    internal class BeesEventHandler : EnemyEventHandler
    {
        public override void OnDeathShrinking(float previousSize, PlayerControllerB playerShrunkenBy)
        {
            Plugin.Log("Bees shrunken to death");

            Effects.LightningStrikeAtPosition(enemy.transform.position);

            if (PlayerInfo.IsHost)
                enemy.KillEnemyServerRpc(true);
        }

        public override void Shrunken(bool wasShrunkenBefore, PlayerControllerB playerShrunkenBy) { }
        public override void Enlarged(bool wasEnlargedBefore, PlayerControllerB playerEnlargedBy) { }
        public override void ScaledToNormalSize(bool wasShrunken, bool wasEnlarged, PlayerControllerB playerScaledBy) { }

    }
}