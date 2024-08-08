using System.Linq;
using UnityEngine;
using VaSiLi.Helper;


namespace Ubiq.XR
{
    /// <summary>
    /// Uses the object under the cursor.
    /// </summary>
    [RequireComponent(typeof(DesktopHand))]
    public class DistanceObjectDesktopUser : MonoBehaviour
    {
        public IDistanceUseable used;
        public Camera mainCamera;

        private DesktopHand hand;

        private Vector3 hit_position;
        private Vector3 lineRenderer_start;
        private LineRenderer lineRenderer;

        private void Awake()
        {
            hand = GetComponent<DesktopHand>();

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
            if (Input.GetMouseButton(0))
            {
                if (used == null)
                {
                    used = PerformRaycast();
                    lineRenderer_start = hit_position;
                    //lineRenderer.SetPosition(0, hit_position);
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
            else if (Input.GetMouseButtonUp(0) && used != null)
            {
                IDistanceUseable target_used = PerformRaycast();
                if (target_used != null) { 
                    if (used == target_used)
                    {
                        used.DistanceUse(hand);
                    }
                    else
                    {
                        used.DistanceLink(hand, target_used);
                    }
                }
                used = null;
                lineRenderer.enabled = false;
            }
        }

        private IDistanceUseable PerformRaycast()
        {
            var mainCamera = FindCamera();

            RaycastHit hit = new RaycastHit();
            if (!Physics.Raycast(mainCamera.ScreenPointToRay(Input.mousePosition).origin,
                                 mainCamera.ScreenPointToRay(Input.mousePosition).direction, out hit, 100,
                                 Physics.DefaultRaycastLayers)
            )
            {
                hit_position = Vector3.zero;
                return null;
            }
            hit_position = hit.point;

            IDistanceUseable hit_usable = hit.collider.gameObject.GetComponentsInParent<MonoBehaviour>().Where(mb => mb is IDistanceUseable).FirstOrDefault() as IDistanceUseable;
            return hit_usable;
        }

        private Camera FindCamera()
        {
            if (mainCamera != null)
            {
                return mainCamera;
            }

            return Camera.main;
        }
    }
}