using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LittleCompany.compatibility
{
    internal interface IScalingListener
    {
        void AfterEachScale();
        void AtEndOfScaling();
    }
}
