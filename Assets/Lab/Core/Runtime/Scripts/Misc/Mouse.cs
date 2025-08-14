using UnityEngine;

public class Mouse : MonoBehaviour
{
    // References to the mouse and anchor rigidbodies
    public Rigidbody mouseRigidbody;
    public Rigidbody anchor;
    public GameObject cursor;
    public float cursorMoveScale = 0.1f;
    public CircleCursorRaycast circleCursorRaycast;

    void Update()
    {
        if (mouseRigidbody != null && anchor != null && cursor != null)
        {
            // Calculate the vector from anchor to mouse
            Vector3 offset = mouseRigidbody.position - anchor.position;
            // If any offset direction is smaller than 0.1 set to zero
            if (Mathf.Abs(offset.x) < 0.1f) offset.x = 0;
            if (Mathf.Abs(offset.y) < 0.1f) offset.y = 0;
            if (Mathf.Abs(offset.z) < 0.1f) offset.z = 0;
            // Scale the offset by the cursor move scale
            offset *= cursorMoveScale;
            cursor.transform.position += offset;
            circleCursorRaycast.UpdateCirclePosition(new Vector2(-offset.x, -offset.z)*10);
        }
    }
}
