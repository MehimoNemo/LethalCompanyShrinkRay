using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LCShrinkRay.comp
{
    internal class TestScript : GrabbableObject
    {

        public override void Start()
        {
            base.Start();
            Debug.Log("Start method called");
        }

        public override void Update()
        {
            base.Update();
            Debug.Log("OnEnable method called");
        }

        protected override string __getTypeName()
        {
            return "TestScript";
        }
    }
}
