using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Linq;
using UnityEngine.XR;
using VaSiLi.Helper;

namespace Ubiq.XR
{
    public class DistanceObjectUser : MonoBehaviour
    {
        //TODO delegates for handyness
        //public delegate void OnDistanceUse(Hand controller, MonoBehaviour go);
        //public delegate void OnDistanceLink(Hand controller, MonoBehaviour go);

        public HandController controller;

        private IDistanceUseable used;

        private Vector3 hit_position;
        private Vector3 lineRenderer_start;
        private LineRenderer lineRenderer;

        private void Awake()
        {
            if (!TryGetComponent<LineRenderer>(out lineRenderer))
                lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        private void Start()
        {
            lineRenderer.enabled = false;
            lineRenderer.widthMultiplier = 0.01f;
            lineRenderer.material.color = Color.red;
            lineRenderer.positionCount = 6;
        }

        private void Update()
        {
            TestDistanceUse();
        }

        private void TestDistanceUse()
        {
            if (controller.TriggerState)
            {
                if (used == null)
                {
                    used = PerformRaycast();
                    lineRenderer_start = hit_position;
                }
                else
                {
                    IDistanceUseable target_used = PerformRaycast();
                    if (target_used != null)
                    {
                        Vector3[] positions = BezierCurve.CalculateCurvePoints(lineRenderer_start, hit_position, 6);
                        lineRenderer.SetPositions(positions);
                        if (target_used != used)
                            lineRenderer.material.color = Color.green;
                        else
                            lineRenderer.material.color = Color.red;
                        lineRenderer.enabled = true;
                    }
                    else
                    {
                        lineRenderer.enabled = false;
                    }
                }
            }
            else if (used != null)
            {
                IDistanceUseable target_used = PerformRaycast();
                if (target_used != null)
                {
                    if (used == target_used)
                    {
                        Debug.Log("!!!!!Using " + used);
                        used.DistanceUse(controller);
                    }
                    else
                    {
                        Debug.Log("!!!!!Linking " + used + " to " + target_used);
                        used.DistanceLink(controller, target_used);
                    }
                }
                used = null;
                lineRenderer.enabled = false;
            }
        }

        private IDistanceUseable PerformRaycast()
        {   
            var Rotation = transform.rotation;
            var Forward = Rotation * Vector3.forward;
            var ray = new Ray(transform.position, Forward);

            var distance = 100f;
            RaycastHit rayHit;
            if (Physics.Raycast(ray, out rayHit, distance, 
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                distance = rayHit.distance;
            }
            else
            {
                return null;
            }

            IDistanceUseable used_hit = rayHit.collider.gameObject.GetComponentsInParent<MonoBehaviour>().Where(mb => mb is IDistanceUseable).FirstOrDefault() as IDistanceUseable;
            return used_hit;
        }
    }
}