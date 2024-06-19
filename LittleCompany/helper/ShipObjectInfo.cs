namespace LittleCompany.helper
{
    internal class ShipObjectInfo
    {
        /*
            foreach (var unlockable in Resources.FindObjectsOfTypeAll<PlaceableShipObject>())
                Plugin.Log(unlockable.parentObject.name + ": " + unlockable.unlockableID);
        */
        public enum VanillaShipObject
        {
            RecordPlayerContainer = 12,
            WelcomeMatContainer = 21,
            PumpkinUnlockableContainer = 20,
            TelevisionContainer = 6,
            Teleporter = 5,
            InverseTeleporter = 19,
            FishBowlContainer = 22,
            Shower = 10,
            PlushiePJManContainer = 23,
            DiscoBallContainer = 27,
            RomanticTableContainer = 14,
            ShipHorn = 18,
            SignalTranslator = 17,
            NormalTableContainer = 13,
            Toilet = 9,
            LightSwitchContainer = 11,
            StorageCloset = 7,
            FileCabinet = 8,
            Bunkbeds = 15,
            Terminal = 16
        }
    }
}
