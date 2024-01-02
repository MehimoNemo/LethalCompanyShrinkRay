using LCShrinkRay.comp;
using LCShrinkRay.Config;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LCShrinkRay.coroutines
{
    internal class RenderVents : MonoBehaviour
    {
        public GameObject go { get; private set; }

        public static void StartRoutine(GameObject gameObject, MeshRenderer[] renderers)
        {
            var routine = gameObject.AddComponent<RenderVents>();
            routine.go = gameObject;
            routine.StartCoroutine(routine.run(renderers));
        }

        private IEnumerator run(MeshRenderer[] renderers)
        {
            float delay = 1f;
            yield return new WaitForSeconds(delay);
            foreach (MeshRenderer renderer in renderers)
            {
                renderer.enabled = true;
            }
        }
    }
}
