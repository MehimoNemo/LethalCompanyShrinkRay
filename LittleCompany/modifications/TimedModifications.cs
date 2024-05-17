using GameNetcodeStuff;
using System;
using System.Collections;
using System.Collections.Generic;
using static LittleCompany.modifications.Modification;
using UnityEngine;

namespace LittleCompany.modifications
{
    internal class TimedModifications<T> where T : MonoBehaviour
    {
        #region Properties
        private static List<TimedModification> _timedModifications = new List<TimedModification>();
        #endregion

        #region Methods
        private static bool TryGetTimedModification(T target, ModificationType type, out TimedModification timedModification)
        {
            foreach (var tm in _timedModifications)
            {
                if (tm.target == target && tm.type == type)
                {
                    timedModification = tm;
                    return true;
                }
            }
            timedModification = null;
            return false;
        }
        #endregion

        class TimedModification
        {
            #region Properties
            public T target { get; set; }
            public PlayerControllerB playerModifiedBy { get; set; }
            public float remainingTime { get; set; }
            public Coroutine coroutine { get; set; }
            public ModificationType type { get; set; }
            #endregion

            public TimedModification(T target, PlayerControllerB playerModifiedBy, float remainingTime, ModificationType type)
            {
                this.target = target;
                this.playerModifiedBy = playerModifiedBy;
                this.remainingTime = remainingTime;
                this.type = type;
            }

            #region Methods
            public void Start()
            {
                if (coroutine != null) return;

                coroutine = target.StartCoroutine(ModificationCoroutine());
            }

            public IEnumerator ModificationCoroutine()
            {
                Plugin.Log("TimedModification initiated. Duration " + (remainingTime / 60f) + " minutes.");

                bool modificationApplied = false;
                ApplyModificationTo(target, type, playerModifiedBy, () => modificationApplied = true);
                yield return new WaitUntil(() => modificationApplied);

                Plugin.Log("TimedModification started.");
                // todo: add circle

                while (remainingTime > 0)
                {
                    remainingTime -= Time.deltaTime;
                    yield return null;
                }

                ModificationType reversedType = ModificationType.Normalizing;
                switch (type)
                {
                    case ModificationType.Shrinking:
                        reversedType = ModificationType.Enlarging;
                        break;
                    case ModificationType.Enlarging:
                        reversedType = ModificationType.Shrinking;
                        break;
                }

                ApplyModificationTo(target, reversedType, playerModifiedBy, () => modificationApplied = true);
                yield return new WaitUntil(() => modificationApplied);

                Plugin.Log("TimedModification ran out of time.");
                _timedModifications.Remove(this);
            }
            #endregion
        }
    }
}
