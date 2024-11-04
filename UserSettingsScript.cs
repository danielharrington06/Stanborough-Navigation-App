using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserSettingsScript : MonoBehaviour
{
    [SerializeField] public bool floor;

    // Start is called before the first frame update
    void Start()
    {
        floor = false; // false is floor 0, true is floor 1
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
