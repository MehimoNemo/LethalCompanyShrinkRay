using static LittleCompany.events.enemy.EnemyEventManager;

namespace LittleCompany.events.enemy
{
    internal class SpiderEventHandler : EnemyEventHandler
    {
        public override void OnDeathShrinking()
        {
            base.OnDeathShrinking();
            Plugin.Log("Spider shrunken to death");
        }
        public override void Shrunken(bool wasShrunkenBefore) { }
        public override void Enlarged(bool wasEnlargedBefore) { }
        public override void ScaledToNormalSize(bool wasShrunken, bool wasEnlarged) { }
    }
}
