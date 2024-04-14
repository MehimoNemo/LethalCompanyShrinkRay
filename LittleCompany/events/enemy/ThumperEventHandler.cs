using GameNetcodeStuff;
using LittleCompany.helper;
using UnityEngine;
using static LittleCompany.events.enemy.EnemyEventManager;

namespace LittleCompany.events.enemy
{
    internal class ThumperEventHandler : EnemyEventHandler
    {
        public override void OnAwake()
        {
            DeathPoofScale = 3f;
            base.OnAwake();
        }

        public override void AboutToDeathShrink(float currentSize, PlayerControllerB playerShrunkenBy)
        {
            // todo: Screaming sound
            var thumper = enemy as CrawlerAI;
            int num = Random.Range(0, thumper.longRoarSFX.Length);
            thumper.creatureVoice.PlayOneShot(thumper.longRoarSFX[num]);

            base.AboutToDeathShrink(currentSize, playerShrunkenBy);
        }

        public override void OnDeathShrinking(float previousSize, PlayerControllerB playerShrunkenBy)
        {
            Plugin.Log("Thumper shrunken to death");

            var thumper = enemy as CrawlerAI;
            // todo: thumping sound (thumper.hitWallSFX), spawn quicksand
            GameObject quicksand = Instantiate(RoundManager.Instance.quicksandPrefab, enemy.transform.position + Vector3.up, Quaternion.identity, RoundManager.Instance.mapPropsContainer.transform);
            if(quicksand.TryGetComponent(out MeshRenderer meshRenderer))
                meshRenderer.material.color = Color.red;

            base.OnDeathShrinking(previousSize, playerShrunkenBy);
        }

    }
}
