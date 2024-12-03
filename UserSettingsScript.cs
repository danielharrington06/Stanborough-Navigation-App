using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserSettingsScript : MonoBehaviour
{
    [SerializeField] public bool floor;

    void Start()
    {
        floor = false; // false is floor 0, true is floor 1
    }
}
