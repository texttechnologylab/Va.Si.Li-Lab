using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.XR;
using Ubiq.Spawning;
using Ubiq.Messaging;

namespace VaSiLi.Object.Drawing
{
    /// <summary>
    /// TODO: Class that allows to draw a line a long the cursor 
    /// </summary>
    public class DrawCursor : MonoBehaviour
    {
        // Start is called before the first frame update
        XRPlayerController controller;
        XRUIRaycasterCursor[] xrCursors;
        public GameObject localDrawing;
        private GameObject currentDrawing;
        DesktopRaycasterCursor desktopCursor;
        public GameObject drawingPrefab;
        void Start()
        {
            controller = XRPlayerController.Singleton;
            xrCursors = controller.gameObject.GetComponentsInChildren<XRUIRaycasterCursor>();
            desktopCursor = controller.gameObject.GetComponentInChildren<DesktopRaycasterCursor>();
            // Set the local drawing parent to the desktop cursor
            localDrawing.transform.parent = desktopCursor.renderer.transform;
            localDrawing.transform.localPosition = Vector3.zero;
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (desktopCursor.renderer.enabled)
                    BeginDrawing();

            }
            if (!desktopCursor.renderer.enabled || Input.GetMouseButtonUp(0))
                EndDrawing();
        }


        private void BeginDrawing()
        {
            localDrawing.GetComponent<LineTrail>().SetEmitting(true, true);

            // Spawn the drawing that will persist
            currentDrawing = NetworkSpawnManager.Find(this).SpawnWithPeerScope(drawingPrefab);
        }

        private void EndDrawing()
        {
            if (!currentDrawing)
                return;

            var trailRenderer = localDrawing.GetComponent<TrailRenderer>();
            Vector3[] positions = new Vector3[trailRenderer.positionCount];
            trailRenderer.GetPositions(positions);

            // Make sure to stop the current element
            localDrawing.GetComponent<LineTrail>().SetEmitting(false, true);
            trailRenderer.Clear();

            // Set the position the new drawing
            currentDrawing.GetComponent<LineTrail>().SetPositions(positions, localDrawing.transform.position, localDrawing.transform.rotation, true);
            currentDrawing = null;
        }
    }
}
