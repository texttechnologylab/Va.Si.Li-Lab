using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CircleCursorRaycast : MonoBehaviour
{
    public float speed = 5f;                 // Speed of cursor movement
    public Canvas canvas;                    // Reference to the canvas
    public KeyCode clickKey = KeyCode.Space; // Key to simulate click
    public Image cursorCircle;               // The circle image that acts as the cursor

    private RectTransform rectTransform;     // To move the cursor (circle)
    private PointerEventData pointerEventData; // Pointer data to simulate clicks
    private EventSystem eventSystem;
    void Start()
    {
        rectTransform = cursorCircle.GetComponent<RectTransform>();
        pointerEventData = new PointerEventData(EventSystem.current);
        eventSystem = EventSystem.current;
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 currentPosition = rectTransform.localPosition;

        // Handle the click simulation with the spacebar (or any other key you want)
        if (Input.GetKeyDown(clickKey))
        {
            // Set up the pointer event data based on the circle's position
            pointerEventData.position = currentPosition;

            // Raycast from the circle position (shoot ray along Z-axis perpendicular to canvas)
            Ray ray = new Ray(cursorCircle.transform.position, Vector3.forward); // Perpendicular to the canvas
            RaycastHit hit;

            // Perform the raycast to check for UI interactions
            if (Physics.Raycast(ray, out hit))
            {
                // For debugging: Show where the ray hits
                Debug.Log("Raycast hit at: " + hit.point);

                // Trigger UI interaction based on raycast hit
                ExecuteEvents.Execute(pointerEventData.pointerCurrentRaycast.gameObject, pointerEventData, ExecuteEvents.pointerClickHandler);
            }
        }
    }
    
    public void UpdateCirclePosition(Vector2 movementDelta)
    {
        // Get the current position of the circle (from RectTransform)
        Vector2 currentPosition = rectTransform.localPosition;

        // Apply the movement delta
        currentPosition += movementDelta;

        // Clamp the position to stay within the canvas bounds
        Vector2 canvasSize = canvas.GetComponent<RectTransform>().sizeDelta;
        currentPosition.x = Mathf.Clamp(currentPosition.x, -canvasSize.x / 2, canvasSize.x / 2);
        currentPosition.y = Mathf.Clamp(currentPosition.y, -canvasSize.y / 2, canvasSize.y / 2);

        // Set the new position to the circle
        rectTransform.localPosition = currentPosition;
    }
}
