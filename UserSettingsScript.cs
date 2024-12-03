using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserSettingsScript : MonoBehaviour
{
    public bool floor;
    public string userType; // "student" "sixth_form", "teacher"


    void Start()
    {
        floor = false; // false is floor 0, true is floor 1
        userType = "student";
    }
}
