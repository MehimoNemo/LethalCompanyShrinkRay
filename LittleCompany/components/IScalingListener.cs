using GameNetcodeStuff;

namespace LittleCompany.components
{
    internal interface IScalingListener
    {
        void AfterEachScale(float from, float to, PlayerControllerB playerBy) { }
        void AtEndOfScaling(float from, float to, PlayerControllerB playerBy) { }
    }
}