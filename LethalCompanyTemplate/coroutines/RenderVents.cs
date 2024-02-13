using System.Collections;
using UnityEngine;

namespace LCShrinkRay.coroutines
{
    internal class RenderVents : MonoBehaviour
    {
        public GameObject bindingObject { get; private set; }

        public static void StartRoutine(GameObject bindingObject)
        {
            var routine = bindingObject.AddComponent<RenderVents>();
            routine.bindingObject = bindingObject;
            routine.StartCoroutine(routine.Run());
        }

        private IEnumerator Run()
        {
            yield return new WaitForSeconds(1f);

            var vents = FindObjectsOfType<EnemyVent>();
            foreach(var vent in vents)
            {
                if(vent == null) continue;

                var ventTunnel = vent.gameObject?.transform.Find("ventTunnel")?.gameObject;
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
