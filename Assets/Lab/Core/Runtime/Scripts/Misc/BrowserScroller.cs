
using TLab.WebView;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BrowserScroller : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private enum ScrollDirection
    {
        Up,
        Down
    }

    [SerializeField]
    private Browser browser;
    [SerializeField]
    private ScrollDirection scrollDirection = ScrollDirection.Down;
    private bool isScrolling = false;


    private void Update()
    {
        if (isScrolling && browser != null)
        {
            int scrollAmount = (int)(100 * Time.deltaTime); // Adjust 200f as needed for speed
            browser.ScrollBy(0, scrollDirection == ScrollDirection.Up ? -scrollAmount : scrollAmount);
        }
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        isScrolling = true;
        Debug.Log("Scrolling started.");
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isScrolling = false;
        Debug.Log("Scrolling stopped.");
    }


}