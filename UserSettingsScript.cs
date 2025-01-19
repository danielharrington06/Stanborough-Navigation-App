using UnityEngine;
using UnityEngine.UI;

public class UserSettingsScript : MonoBehaviour
{
    public bool floor;
    public bool mapFocussed;
    public bool stepFree;
    public bool invertScroll;
    [SerializeField] private Text floorText;

    void Start(){
        floor = false; // false is floor 0, true is floor 1
        mapFocussed = true;
        stepFree = false;
        invertScroll = false;
        floorText.text = "0";
    }

    public void SetFloor0() {
        floor = false;
        floorText.text = "0";
    }

    public void SetFloor1() {
        floor = true;
        floorText.text = "1";
    }

    public void SetMapUnfocussed() {
        mapFocussed = false;
    }

    public void SetMapFocussed() {
        mapFocussed = true;
    }
}
