namespace LittleCompany.helper
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
            Adamance = 5,
            Rend = 6,
            Dine = 7,
            Offense = 8,
            Titan = 9,
            Artifice = 10,
            Liquidation = 11,
            Embrion = 12
        }

#if DEBUG
        /*
        for (int i = 0; i < RoundManager.Instance.dungeonFlowTypes.Length; i++)
            {
                Plugin.Log("Dungeon: " + RoundManager.Instance.dungeonFlowTypes[i].dungeonFlow.name + " = " + i);
            }
        */
        public enum Dungeon
        {
            Level1Flow = 0,           // Factory
            Level2Flow = 1,           // Haunted Mansion
            Level1FlowExtraLarge = 2, // Large Factory
            Level1Flow3Exit = 3,      // Factory 3 exit
            Level3Flow = 4,           // Mineshaft
        }
#endif
    }
}
