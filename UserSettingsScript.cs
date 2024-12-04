using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class UserSettingsScript : MonoBehaviour
{
    public bool floor;
    public bool stepFree;

    void Start()
    {
        floor = false; // false is floor 0, true is floor 1
        stepFree = true;
    }
}
