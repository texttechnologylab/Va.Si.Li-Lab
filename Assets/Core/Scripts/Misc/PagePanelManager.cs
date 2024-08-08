using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Samples;
using Ubiq.Samples.Social;
using TMPro;
public class PagePanelManager : MonoBehaviour
{
    public PagePanel pagePanel;
    public TMP_Text textPanel;
    // Start is called before the first frame update
    void Start()
    {
        pagePanel.SetPageCount(100);
        pagePanel.SetPage(0);
    }

    public void SetPage(int page, int pageCount)
    {
        textPanel.pageToDisplay = page + 1;
    }

}
