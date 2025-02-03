using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TextToiletRendererScript : MonoBehaviour
{
    [SerializeField] private DatabaseHelperScript databaseHelper;
    [SerializeField] private UserSettingsScript userSettings;
    [SerializeField] private GameObject worldSpaceCanvas;
    [SerializeField] private GameObject textPrefab;
    [SerializeField] private GameObject[] toiletPrefabs;

    private bool floor;
    private bool mapFocussed;

    private List<string[]> textLabelData;
    private List<GameObject> createdTextLabels;

    private List<string[]> toiletSymbolData;
    private List<GameObject> createdToiletSymbols;
    
    void Start() {
        
        floor = userSettings.floor;
        mapFocussed = userSettings.mapFocussed;

        textLabelData = new List<string[]>();
        createdTextLabels = new List<GameObject>();
        
        toiletSymbolData = new List<string[]>();
        createdToiletSymbols = new List<GameObject>();

        textLabelData = databaseHelper.GetTextLabels(floor);
        InstantiateTextLabels();
        toiletSymbolData = databaseHelper.GetToiletSymbols(floor);
        InstantiateToiletSymbols();
    }

    void Update() {
        // check for change in map focus
        if (userSettings.mapFocussed != mapFocussed) {
            mapFocussed = userSettings.mapFocussed;
            if (!mapFocussed) { // changed to unfocussed so destroy previous text and toilet
                DestroyTextLabels();
                DestroyToiletSymbols();
            }
            else { // changed to focussed so check db and redo text and toilet
                textLabelData = databaseHelper.GetTextLabels(floor);
                InstantiateTextLabels();
                toiletSymbolData = databaseHelper.GetToiletSymbols(floor);
                InstantiateToiletSymbols();
            }
        }

        // when floor is toggled, if map is focussed, redo text and toilets
        if (userSettings.floor != floor && userSettings.mapFocussed) {
            floor = userSettings.floor;

            // destroy current then generate new text labels for new floor
            DestroyTextLabels();
            textLabelData = databaseHelper.GetTextLabels(floor);
            InstantiateTextLabels();

            // destroy current then generate new toilet symbols for new floor
            DestroyToiletSymbols();
            toiletSymbolData = databaseHelper.GetToiletSymbols(floor);
            InstantiateToiletSymbols();
        }
    }

    /**
    This function destoys all current text labels.
    */
    private void DestroyTextLabels() {
        for (int i = 0; i < createdTextLabels.Count; i++) {
            Destroy(createdTextLabels[i]);
        }
    }

    /**
    This function generates new text labels and instantiates them.
    */
    public void InstantiateTextLabels() {
        for (int i = 0; i < textLabelData.Count; i++) {
            // instantiate the text prefab
            GameObject textObject = Instantiate(textPrefab, transform);

            // make child of world space canvas
            textObject.transform.SetParent(worldSpaceCanvas.transform, false);
            
            // adjust position
            textObject.transform.position = new Vector3(Convert.ToSingle(textLabelData[i][1]), Convert.ToSingle(textLabelData[i][2]), transform.position.z);

            // change game object name
            textObject.name = "TextLabel " + textLabelData[i][0];

            // get the tmp component
            TextMeshProUGUI textComponent = textObject.GetComponent<TextMeshProUGUI>();
            if (textComponent != null) {
                // set the text content
                textComponent.text = textLabelData[i][0];
                // set the font size
                textComponent.fontSize = Convert.ToSingle(textLabelData[i][3]);

                // set the width and height by adjusting the rect transform
                RectTransform rectTransform = textComponent.GetComponent<RectTransform>();
                if (rectTransform != null) {
                    rectTransform.sizeDelta = new Vector2(Convert.ToSingle(textLabelData[i][4]), Convert.ToSingle(textLabelData[i][5]));
                }

            }

            // add to the list of created labels
            createdTextLabels.Add(textObject);
        }
    }

    /**
    This function destoys all current toilet symbols.
    */
    private void DestroyToiletSymbols() {
        for (int i = 0; i < createdToiletSymbols.Count; i++) {
            Destroy(createdToiletSymbols[i]);
        }
    }

    /**
    This function generates new text labels and instantiates them.
    */
    public void InstantiateToiletSymbols() {
        for (int i = 0; i < toiletSymbolData.Count; i++) {
            // instantiate the toilet prefab according to which type it is
            GameObject toiletObject;
            switch (toiletSymbolData[i][3]) {
                case "B": // boys toilet
                    toiletObject = Instantiate(toiletPrefabs[0], transform);
                    break;
                case "G": // girls toilet
                    toiletObject = Instantiate(toiletPrefabs[1], transform);
                    break;
                default: // mixed toilet so generic symbol
                    toiletObject = Instantiate(toiletPrefabs[2], transform);
                    break;
            }

            // make child of world space canvas
            toiletObject.transform.SetParent(worldSpaceCanvas.transform, false);
            
            // adjust position
            toiletObject.transform.position = new Vector3(Convert.ToSingle(toiletSymbolData[i][1]), Convert.ToSingle(toiletSymbolData[i][2]), 0);

            // change game object name
            toiletObject.name = "ToiletSymbol " + textLabelData[i][0] + " " + toiletSymbolData[i][3];

            // add to the list of created labels
            createdToiletSymbols.Add(toiletObject);
        }
    }


}
