using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.XR;

public class ReadableBook : MonoBehaviour, IUseable
{
    public GameObject BookMenu;

    void IUseable.Use(Hand controller)
    {
        BookMenu.SetActive(true);
    }

    void IUseable.UnUse(Hand controller)
    {
    }
}
