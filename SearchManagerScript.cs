using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;

public class SearchManagerScript : MonoBehaviour
{
    public bool searchOpen;
    public float searchPanelMinX {get; private set;}

    void Start() {
        searchOpen = false; // search panel closed at first
        searchPanelMinX = -3.36f; // update as needed if search panel changes
        
    }

    public void SetSearchOpen() {
        searchOpen = true;
    }

    public void SetSearchClosed() {
        searchOpen = false;
    }
}
