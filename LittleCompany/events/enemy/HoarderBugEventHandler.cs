using static LittleCompany.events.enemy.EnemyEventManager;

namespace LittleCompany.events.enemy
{
    internal class HoarderBugEventHandler: EnemyEventHandler
    {
        public override void OnDeathShrinking(float previousSize)
        {
            base.OnDeathShrinking(previousSize);
            Plugin.Log("Hoarderbug shrunken to death");
        }
        public override void Shrunken(bool wasShrunkenBefore) { }
        public override void Enlarged(bool wasEnlargedBefore) { }
        public override void ScaledToNormalSize(bool wasShrunken, bool wasEnlarged) { }
    }
}
