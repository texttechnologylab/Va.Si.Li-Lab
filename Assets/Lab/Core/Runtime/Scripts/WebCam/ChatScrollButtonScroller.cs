using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Attach to a scroll-up or scroll-down button. While the button is held, the target
/// ScrollRect scrolls continuously — the same hold-to-scroll behaviour as BrowserScroller.
/// </summary>
public class ChatScrollButtonScroller : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public enum ScrollDirection { Up, Down }

    [SerializeField] ScrollRect scrollRect;
    [SerializeField] ScrollDirection direction = ScrollDirection.Down;
    [Tooltip("Scroll units per second (normalised 0–1 range of the ScrollRect).")]
    [SerializeField] float scrollSpeed = 0.5f;

    bool _scrolling;

    void Update()
    {
        if (!_scrolling || scrollRect == null) return;
        float delta = scrollSpeed * Time.deltaTime;
        scrollRect.verticalNormalizedPosition = Mathf.Clamp01(
            scrollRect.verticalNormalizedPosition + (direction == ScrollDirection.Up ? delta : -delta));
    }

    public void OnPointerDown(PointerEventData _) => _scrolling = true;
    public void OnPointerUp(PointerEventData _)   => _scrolling = false;
}
