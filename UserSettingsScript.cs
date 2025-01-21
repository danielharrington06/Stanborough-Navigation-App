using Christina.UI;
using UnityEngine;
using UnityEngine.UI;

public class UserSettingsScript : MonoBehaviour
{   
    [Header ("UI Elements")]
    [SerializeField] private Text floorText;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private ToggleSwitchColorChange OWSToggle;
    [SerializeField] private ToggleSwitchColorChange SFToggle;
    [SerializeField] private ToggleSwitchColorChange UTEToggle;
    [SerializeField] private ToggleSwitchColorChange ISToggle;   

    [Header ("Non-User Settings")]
    public bool floor;
    public bool mapFocussed;

    [Header ("User Settings")]
    public bool oneWaySystem;
    public bool stepFree;
    public bool useTimeOfDayForCalculation;
    public bool invertScroll;
    
    void Start(){
        floor = false; // false is floor 0, true is floor 1
        mapFocussed = true;
        floorText.text = "0";
        ResetSettings();
    }

    public void ResetSettings() {
        oneWaySystem = true;
        stepFree = false;
        useTimeOfDayForCalculation = true;
        invertScroll = false;
        
        if (settingsPanel.activeSelf) { // check if settingsPanel is active
            OWSToggle.SetStateAndStartAnimation(oneWaySystem);
            SFToggle.SetStateAndStartAnimation(stepFree);
            UTEToggle.SetStateAndStartAnimation(useTimeOfDayForCalculation);
            ISToggle.SetStateAndStartAnimation(invertScroll);
        }
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

    public void SetOneWaySystemOn() {
        oneWaySystem = true;
        stepFree = false; // as OWS being turned on means step free should be off
        SFToggle.SetStateAndStartAnimation(stepFree);
    }

    public void SetOneWaySystemOff() {
        oneWaySystem = false;
    }

    public void SetStepFreeOn() {
        stepFree = true;
        oneWaySystem = false; // as step free doesnt use OWS
        OWSToggle.SetStateAndStartAnimation(oneWaySystem);
    }

    public void SetStepFreeOff() {
        stepFree = false;
        oneWaySystem = true; // as this should be the default
        OWSToggle.SetStateAndStartAnimation(oneWaySystem);
    }

    public void SetUseTimeOfDayForCalculationOn() {
        useTimeOfDayForCalculation = true;
    }

    public void SetUseTimeOfDayForCalculationOff() {
        useTimeOfDayForCalculation = false;
    }

    public void SetInvertScrollOn() {
        invertScroll = true;
    }

    public void SetInvertScrollOff() {
        invertScroll = false;
    }
}
