using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ReadableBookMenu : MonoBehaviour
{
    public Button NextPageButton;
    public Button PreviousPageButton;
    public Transform PageContainer;

    private int currentPage = 0;

    private void OnEnable(){
        SetPage(0);
    }

    private void SetPage(int page){
        currentPage = page;
        for(int i=0; i<PageContainer.childCount; i++){
            PageContainer.GetChild(i).gameObject.SetActive(i==currentPage);
        }
        UpdateButtonState();
    }

    private void UpdateButtonState(){
        PreviousPageButton.gameObject.SetActive(currentPage > 0);
        NextPageButton.gameObject.SetActive(currentPage < PageContainer.childCount-1);
    }

    public void NextPage(){
        SetPage(currentPage+1);
    }

    public void PreviousPage(){
        SetPage(currentPage-1);
    }
}
