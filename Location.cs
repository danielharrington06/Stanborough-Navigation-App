using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Location : MonoBehaviour
{
    public string id; // text eg "G4" or "18" or "MH"
    public int node; // starting node
    public string type; // "" if undefined, "N" if node, "RN" if room that is a node, "RNC" if room thats connected to a node, "REU" if undirectional edge, "RED" if directional edge"
    public bool floor; // false means 0, true means 1
    public Vector2 coordinates; // world spacee coordinates
    public string userText; // user input text
    private DatabaseHelperScript databaseHelper = new DatabaseHelperScript();

    public Location() {
        ResetFields();
    }

    public void ResetFields() {
        id = "";
        node = -1;
        type = "   ";
        floor = false;
        coordinates = new Vector2(0, 0);
        userText = "";
    }

    public void SetupLocation() {
        if (id != "") {
            type = databaseHelper.GetLocationType(id);
            floor = databaseHelper.GetLocationFloor(id, type);
            coordinates = databaseHelper.GetLocationCoordinates(id, type);
        }
        else {
            ResetFields();
        }
        
    }
}
