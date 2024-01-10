using System.Collections;
using UnityEngine;

namespace LCShrinkRay.coroutines
{
    internal class RenderVents : MonoBehaviour
    {
        public GameObject go { get; private set; }

        public static void StartRoutine(GameObject gameObject)
        {
            var routine = gameObject.AddComponent<RenderVents>();
            routine.go = gameObject;
            routine.StartCoroutine(routine.run());
        }

        private IEnumerator run()
        {
            yield return new WaitForSeconds(1f);

            var vents = UnityEngine.Object.FindObjectsOfType<EnemyVent>();
            foreach(var vent in vents)
            {
                var gameObject = vent.gameObject.transform.Find("Hinge").gameObject.transform.Find("VentCover").gameObject;
                if(gameObject == null)
                {
                    Plugin.log("A vent gameObject was null.");
                    continue;
                }
                var meshRenderer = gameObject.GetComponent<MeshRenderer>();
                if (meshRenderer == null)
                {
                    Plugin.log("A vent mesh renderer was null.");
                    continue;
                }

                meshRenderer.enabled = true;
            }
        }
    }
}
