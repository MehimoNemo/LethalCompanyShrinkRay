using System;
using System.Collections.Generic;
using System.Text;

namespace LCShrinkRay.helper
{
    internal class Moons
    {
        /*
        foreach (var level in StartOfRound.Instance.levels)
        {
            Plugin.Log(level.name + " = " + level.levelID + ",");
        }
        */
        public enum Moon
        {
            Experimentation = 0,
            Assurance = 1,
            Vow = 2,
            CompanyBuilding = 3,
            March = 4,
            Rend = 5,
            Dine = 6,
            Offense = 7,
            Titan = 8
        }


        /*
        for (int i = 0; i < RoundManager.Instance.dungeonFlowTypes.Length; i++)
        {
            Plugin.Log("Dungeon: " + RoundManager.Instance.dungeonFlowTypes[i].name + " = " + i);
        }
        */
        public enum Dungeon
        {
            Level1Flow = 0,             // Factory
            Level2Flow = 1,             // Haunted Mansion
            Level1FlowExtraLarge = 2    // Large Factory
        }
    }
}
