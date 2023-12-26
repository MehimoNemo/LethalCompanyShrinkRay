using BepInEx.Logging;
using GameNetcodeStuff;
using System;
using UnityEngine;

namespace LCShrinkRay.comp
{
    internal class ShrinkRay : GrabbableObject
    {
        private ManualLogSource mls;
        private PlayerControllerB previousPlayerHeldBy;
        private RaycastHit[] enemyColliders;
        Shrinking shrinking;
        GameObject beamObject;
        LineRenderer lineRenderer;


        public Material beamMaterial;
        public float beamWidth = 0.1f;
        public float beamLength = 10f;
        public float beamDuration = 2f;
        //private Color beamColor = Color.blue;




        public override void Start()
        {
            base.Start();
            this.itemProperties.requiresBattery = false;
            this.useCooldown = 0.5f;
            enemyColliders = new RaycastHit[10];
            mls = BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_GUID);
            mls.LogMessage("STARTING SHRINKRAY");

            shrinking = new Shrinking();

            beamMaterial = new Material(Shader.Find("HDRP/Unlit"));
            /*// Set the emission color
            beamMaterial.SetColor("_EmissionColor", Color.blue);
            // Enable emission
            beamMaterial.EnableKeyword("_EMISSION");*/
            Texture2D blueTexture = new Texture2D(1, 1);
            blueTexture.SetPixel(0, 0, Color.blue);
            blueTexture.Apply();
            if (beamMaterial == null)
            {
                mls.LogError("FUCKER DAMNIT SHIT ASS");
            }
            //beamMaterial.mainTexture = blueTexture;
            //beamMaterial.color = beamColor;
        }



        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            try
            {
                if (shrinking == null)
                {
                    mls.LogMessage("shrinking is null...");
                    shrinking = new Shrinking();
                }
                mls.LogMessage("triggering SHRINKRAY");
                base.ItemActivate(used, buttonDown);
                if (beamObject == null || beamObject.gameObject == null)
                {
                    ShootRayAndSync();
                }
            }
            catch (Exception e) { }
        }

        public float duration = 0.6f;

        private float elapsedTime = 0f;

        public Color startColor = Color.blue;
        public Color endColor = Color.cyan;

        public override void Update() {
            base.Update();

            if (shrinking == null)
            {
                shrinking = new Shrinking();
            }

                //if beam exists
                try
                {
                if (beamObject != null && lineRenderer != null && this.playerHeldBy != null && this.playerHeldBy.gameplayCamera != null)
                {
                    Transform transform = this.playerHeldBy.gameplayCamera.transform;
                    Vector3 beamStartPos;
                    Vector3 forward;

                    

                    beamStartPos = transform.position - transform.up * 0.1f;
                    forward = transform.forward;
                    forward = forward * beamLength + beamStartPos;

                    //offset the ding dang beam a lil to the right 
                    beamStartPos += transform.right * 0.35f;
                    forward += transform.right * 0.35f;

                    //offset the beam a lil bit forwards
                    beamStartPos += transform.forward * 1.3f;
                    forward += transform.forward * 1.3f;
                    

                    // Increment the elapsed time based on the frame time
                    elapsedTime += Time.deltaTime;

                    // Calculate the interpolation factor between 0 and 1 based on elapsed time
                    float t = Mathf.Repeat(elapsedTime / duration, 1.0f);
                    float t2 = Mathf.Repeat(elapsedTime + 1f / duration, 1.0f);
                    // Lerp between startColor and endColor
                    Color lerpedColor = Color.Lerp(startColor, endColor, t);
                    Color lerpedColor2 = Color.Lerp(startColor, endColor, t2);

                    // Apply the color to the material or any other component that has color
                    {
                        lineRenderer.endColor = lerpedColor;
                        lineRenderer.startColor = lerpedColor2;
                    }

                    /*Ray ray = new Ray(beamStartPos, forward);
                    RaycastHit hit;
                    float maxRayDistance = beamLength; // Adjust as needed
                    bool hitSomething = Physics.Raycast(ray, out hit, maxRayDistance, StartOfRound.Instance.walkableSurfacesMask);
                    if (hitSomething)
                    {
                        forward -= (hit.distance+1) * transform.forward;
                    }*/


                    lineRenderer.SetPosition(0, beamStartPos);
                    lineRenderer.SetPosition(1, forward);
                }
            }
            catch(Exception e)
            {
                mls.LogMessage(e);
            }
        }

        public void ShootRayAndSync()
        {
            Transform transform = this.playerHeldBy.gameplayCamera.transform;
            
            Vector3 beamStartPos;
            Vector3 forward;



            beamStartPos = transform.position - transform.up * 0.1f;
            forward = transform.forward;
            forward = forward * beamLength + beamStartPos;

            //offset the ding dang beam a lil to the right 
            beamStartPos += transform.right * 0.35f;
            forward += transform.right * 0.35f;

            //offset the beam a lil bit forwards
            beamStartPos += transform.forward * 1.3f;
            forward += transform.forward * 1.3f;


            mls.LogMessage(beamStartPos);
            mls.LogMessage(beamStartPos + forward * beamLength);



            Debug.Log("Calling shoot gun....");
            ShootRay(beamStartPos, forward);
            Debug.Log("Calling shoot gun and sync");
        }

        //do a cool raygun effect, ray gun sound, cast a ray, and shrink any players caught in the ray
        private void ShootRay(Vector3 beamStartPos, Vector3 forward)
        {

            mls.LogMessage("shootingggggg");


            if (enemyColliders == null)
            {
                enemyColliders = new RaycastHit[10];
            }

            Ray ray = new Ray(beamStartPos, beamStartPos + forward * beamLength);
            RenderCoolBeam(beamStartPos, beamStartPos + forward * beamLength);
            int hitEnemiesCount = Physics.SphereCastNonAlloc(ray, 5f, enemyColliders, beamLength, StartOfRound.Instance.playersMask, QueryTriggerInteraction.Collide);
            mls.LogMessage("Casted Ray");
            mls.LogMessage("hitEnemiesCount: " + hitEnemiesCount);
            for (int i = 0; i < hitEnemiesCount; i++) {
                mls.LogMessage("enemycolliderpint: " + enemyColliders[i].point);
                if (Physics.Linecast(beamStartPos, enemyColliders[i].point, out var hitInfo, StartOfRound.Instance.playersMask, QueryTriggerInteraction.Ignore))
                {
                    Debug.DrawRay(hitInfo.point, Vector3.up, Color.red, 15f);
                    Debug.DrawLine(beamStartPos, enemyColliders[i].point, Color.cyan, 15f);
                    mls.LogMessage("Raycast hit wall");
                }
                else
                {
                    PlayerControllerB component;
                    if (enemyColliders[i].transform.TryGetComponent<PlayerControllerB>(out component))
                    {
                        Debug.Log($"Hit enemy,");
                        int targetPlayerID = (int)component.playerClientId;
                        if (component.transform.localScale.x == 1f && component.playerClientId != this.playerHeldBy.playerClientId) {
                            //shrink the target player and also broadcast to other clients
                            shrinking.sendShrinkMessage(component.gameObject, 0.4f);
                            shrinking.ShrinkPlayer(component.gameObject, 0.4f, targetPlayerID.ToString());
                        }
                    }
                    else
                    {
                        Debug.Log("Could not get hittable script from collider, transform: " + enemyColliders[i].transform.name);
                        Debug.Log("collider: " + enemyColliders[i].collider.name);
                    }
                }
            }

        }
        public void RenderCoolBeam(Vector3 beamStartPos, Vector3 forward)
        {
            mls.LogMessage("trying to render cool beam");
            mls.LogMessage("parent is: " + parentObject.gameObject.name);
            
            
            if (parentObject.transform.Find("Beam") == null && beamMaterial != null)
            {
                mls.LogMessage("trying to create beam object");
                beamObject = new GameObject("Beam");
                mls.LogMessage("Before creating LineRenderer");
                lineRenderer = beamObject.AddComponent<LineRenderer>();
                mls.LogMessage("After creating LineRenderer");
                //beamObject.transform.parent = transform;
                lineRenderer.material = beamMaterial;
                lineRenderer.startWidth = beamWidth;
                lineRenderer.endWidth = beamWidth*16;
                lineRenderer.endColor = new Color(0, 0.5f, 0.5f, 0.5f);
                //lineRenderer.material.color = new Color(0f, 0f, 1f, 1f); // Adjust alpha as needed
                lineRenderer.material.renderQueue = 2500; // Adjust as needed

                mls.LogMessage("Adding line renderer");

                //Vector3 beamEndPosition = beamStartPos + forward * beamLength;
                lineRenderer.SetPosition(0, beamStartPos);
                lineRenderer.SetPosition(1, forward);
                lineRenderer.enabled = true;
                lineRenderer.numCapVertices = 6;
                lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lineRenderer.receiveShadows = false;

                try
                {
                    LineRenderer pl = parentObject.GetComponent<LineRenderer>();
                    pl.startColor = new Color(0, 0, 0, 0);
                    pl.endColor = new Color(0, 0, 0, 0);
                }
                catch (Exception e) { }

                //beam does not get deleted no more :)
                Destroy(beamObject, beamDuration);
            }
        }

        public override void EquipItem()
        {
            base.EquipItem();
            previousPlayerHeldBy = playerHeldBy;
            previousPlayerHeldBy.equippedUsableItemQE = true;
        }
        public override void PocketItem()
        {
            base.PocketItem();
        }

        public override void DiscardItem()
        {
            mls.LogMessage("Discarding");
            base.DiscardItem();
        }
    }

}
