using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.XR;

public class MediaScreenButton : MonoBehaviour, IUseable
{
    public enum BUTTON_TYPE
    {
        NEXT = 0,
        PREVIOUS = 1
    }

    public BUTTON_TYPE buttonType;

    public MediaScreen screen;

    void IUseable.Use(Hand controller)
    {
        if(buttonType == BUTTON_TYPE.NEXT){
            screen.LoadNext();
        }
        else{
            screen.LoadPrevious();
        }
    }

    void IUseable.UnUse(Hand controller)
    {
    }

}
