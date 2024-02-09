using System.Collections;
using UnityEngine;

namespace LCShrinkRay.coroutines
{
    internal class RenderVents : MonoBehaviour
    {
        public GameObject gameObject { get; private set; }

        public static void StartRoutine(GameObject gameObject)
        {
            var routine = gameObject.AddComponent<RenderVents>();
            routine.gameObject = gameObject;
            routine.StartCoroutine(routine.Run());
        }

        private IEnumerator Run()
        {
            yield return new WaitForSeconds(1f);

            var vents = FindObjectsOfType<EnemyVent>();
            foreach(var vent in vents)
            {
                if(vent == null) continue;

                var ventTunnel = vent.gameObject.transform.Find("ventTunnel").gameObject;
                if(ventTunnel == null)
                {
                    Plugin.Log("A ventTunnel gameObject was null.");
                    continue;
                }
                var meshRenderer = ventTunnel.GetComponent<MeshRenderer>();
                if (meshRenderer == null)
                {
                    Plugin.Log("A vent mesh renderer was null.");
                    continue;
                }

                meshRenderer.enabled = true;
            }
        }
    }
}
