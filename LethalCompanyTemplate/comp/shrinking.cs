using System;
using System.Collections.Generic;
using UnityEngine;
using GameNetcodeStuff;

using LCShrinkRay.patches;
using LCShrinkRay.Config;
using Unity.Netcode;
using System.Linq;
using Newtonsoft.Json;
using LCShrinkRay.helper;
using LethalLib.Modules;

namespace LCShrinkRay.comp
{
    internal class Shrinking
    {
        private static Shrinking instance = null;
        private static readonly object padlock = new object();

        public static Shrinking Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                        instance = new Shrinking();

                    return instance;
                }
            }
        }

        public Transform helmetHudTransform;

        public void Update()
        {
            if (!GameNetworkManagerPatch.isGameInitialized || !GameNetworkManager.Instance.localPlayerController)
                return;

            if (helmetHudTransform == null)
            {
                if (GameObject.Find("ScavengerHelmet") != null)
                {
                    helmetHudTransform = GameObject.Find("ScavengerHelmet").GetComponent<Transform>();
                    helmetHudTransform.localPosition = new Vector3(-0.0f, 0.058f, -0.274f);
                    Plugin.log("Player transform got!");
                }
            }
        }

        
    }
}
