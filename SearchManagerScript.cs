using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;

public class SearchManagerScript : MonoBehaviour
{
    public bool searchOpen;

    void Start() {
        searchOpen = false; // search panel closed at first
        
    }

    public void SetSearchOpen() {
        searchOpen = true;
    }

    public void SetSearchClosed() {
        searchOpen = false;
    }
}
