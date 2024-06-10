using GameNetcodeStuff;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using LittleCompany.components;
using LittleCompany.helper;
using LittleCompany.modifications;

namespace LittleCompany.events
{
    [DisallowMultipleComponent]
    public abstract class EventHandlerBase : NetworkBehaviour, IScalingListener
    {
        public abstract void DestroyObject();
        public abstract void DespawnObject();

        internal float DeathPoofScale = Effects.DefaultDeathPoofScale;

        void Awake()
        {
            Plugin.Log(name + " event handler has awaken!");
            OnAwake();
        }

        public abstract void OnAwake();

        public void AfterEachScale(float from, float to, PlayerControllerB playerBy)
        {
            if (Mathf.Approximately(from, to)) return;
            Scaling(from, to, playerBy);
            if (from > to)
                Shrinking(from <= 1f, playerBy);
            else
                Enlarging(from >= 1f, playerBy);
        }

        public void AtEndOfScaling(float from, float to, PlayerControllerB playerBy)
        {
            if (Mathf.Approximately(from, to)) return;
            Scaled(from, to, playerBy);
            if (from > to)
                Shrunken(from <= 1f, playerBy);
            else
                Enlarged(from >= 1f, playerBy);
        }

        public virtual void AboutToDeathShrink(float currentSize, PlayerControllerB playerShrunkenBy)
        {
            Plugin.Log("AboutToDeathShrink");
        }

        public virtual void OnDeathShrinking(float previousSize, PlayerControllerB playerShrunkenBy)
        {
            if (Effects.TryCreateDeathPoofAt(out _, gameObject.transform.position, DeathPoofScale) && gameObject.TryGetComponent(out AudioSource audioSource) && audioSource != null && Modification.deathPoofSFX != null)
                audioSource.PlayOneShot(Modification.deathPoofSFX);

            if (PlayerInfo.IsHost)
                StartCoroutine(DespawnAfterEventSync());
            else
                DeathShrinkEventReceivedServerRpc();

            Plugin.Log(name + " shrunken to death");
        }

        #region DeathShrinkSync
        // Has to be done to prevent the enemy from despawning before any client got the OnDeathShrinking event
        private int DeathShrinkSyncedPlayers = 1; // host always got it
        public IEnumerator DespawnAfterEventSync()
        {
            var waitedFrames = 0;
            var playerCount = PlayerInfo.AllPlayers.Count;
            Plugin.Log("EventManager.PlayerCount: " + playerCount);
            while (waitedFrames < 100 && DeathShrinkSyncedPlayers < playerCount)
            {
                waitedFrames++;
                yield return null;
            }

            if (waitedFrames == 100)
                Plugin.Log("Timeout triggered the death shrink event.", Plugin.LogType.Warning);
            else
                Plugin.Log("Syncing the death shrink event took " + waitedFrames + " frames.");

            DeathShrinkSyncedPlayers = 1;

            DestroyObject();

            yield return new WaitForSeconds(3f);
            if (PlayerInfo.IsHost)
                DespawnObject();
        }

        [ServerRpc(RequireOwnership = false)]
        public void DeathShrinkEventReceivedServerRpc()
        {
            DeathShrinkSyncedPlayers++;
        }
        #endregion

        // Realtime methods
        public virtual void Scaling(float from, float to, PlayerControllerB playerShrunkenBy) { }
        public virtual void Shrinking(bool wasAlreadyShrunken, PlayerControllerB playerShrunkenBy) { }
        public virtual void Enlarging(bool wasAlreadyEnlarged, PlayerControllerB playerEnlargedBy) { }

        // Single-Time methods
        public virtual void Scaled(float from, float to, PlayerControllerB playerShrunkenBy) { }
        public virtual void Shrunken(bool wasAlreadyShrunken, PlayerControllerB playerShrunkenBy) { }
        public virtual void Enlarged(bool wasAlreadyEnlarged, PlayerControllerB playerEnlargedBy) { }
        public virtual void ScaledToNormalSize(bool wasShrunken, bool wasEnlarged, PlayerControllerB playerScaledBy) { }
    }
}
