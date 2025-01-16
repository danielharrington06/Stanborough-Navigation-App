using UnityEngine;
using UnityEngine.UI;

public class UserSettingsScript : MonoBehaviour
{
    public bool floor;
    public bool stepFree;
    public bool invertScroll;
    [SerializeField] private Text floorText;

    void Start(){
        floor = false; // false is floor 0, true is floor 1
        stepFree = true;
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
}
