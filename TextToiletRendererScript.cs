using System;
using System.Collections;
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

    private List<string[]> textLabelData;
    private List<GameObject> createdTextLabels;

    private List<string[]> toiletSymbolData;
    private List<GameObject> createdToiletSymbols;
    
    void Start() {
        
        floor = userSettings.floor;

        textLabelData = new List<string[]>();
        createdTextLabels = new List<GameObject>();
        
        toiletSymbolData = new List<string[]>();
        createdToiletSymbols = new List<GameObject>();

        textLabelData = databaseHelper.GetTextLabels(floor);
        GenerateNewTextLabels();
        toiletSymbolData = databaseHelper.GetToiletSymbols(floor);
        GenerateNewToiletSymbols();
    }

    void Update() {

        if (userSettings.floor != floor) {
            floor = userSettings.floor;

            // destroy current then generate new text labels for new floor
            DestroyPreviousTextLabels();
            textLabelData = databaseHelper.GetTextLabels(floor);
            GenerateNewTextLabels();

            // destroy current then generate new toilet symbols for new floor
            DestroyPreviousToiletSymbols();
            toiletSymbolData = databaseHelper.GetToiletSymbols(floor);
            GenerateNewToiletSymbols();
        }
    }

    /**
    This function destoys all current text labels.
    */
    private void DestroyPreviousTextLabels() {
        for (int i = 0; i < createdTextLabels.Count; i++) {
            Destroy(createdTextLabels[i]);
        }
    }

    /**
    This function generates new text labels and instantiates them.
    */
    public void GenerateNewTextLabels() {
        for (int i = 0; i < textLabelData.Count; i++) {
            // instantiate the text prefab
            GameObject textObject = Instantiate(textPrefab, transform);

            // make child of world space canvas
            textObject.transform.SetParent(worldSpaceCanvas.transform, false);
            
            // adjust position
            textObject.transform.position = new Vector3(Convert.ToSingle(textLabelData[i][1]), Convert.ToSingle(textLabelData[i][2]), 0);

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
    private void DestroyPreviousToiletSymbols() {
        for (int i = 0; i < createdToiletSymbols.Count; i++) {
            Destroy(createdToiletSymbols[i]);
        }
    }

    /**
    This function generates new text labels and instantiates them.
    */
    public void GenerateNewToiletSymbols() {
        for (int i = 0; i < toiletSymbolData.Count; i++) {
            // instantiate the toilet prefab according to which type it is
            GameObject toiletObject;
            switch (toiletSymbolData[i][2]) {
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
            toiletObject.transform.position = new Vector3(Convert.ToSingle(toiletSymbolData[i][0]), Convert.ToSingle(toiletSymbolData[i][1]), 0);

            // change game object name
            toiletObject.name = "ToiletSymbol " + textLabelData[i][0] + " " + toiletSymbolData[2];

            // add to the list of created labels
            createdToiletSymbols.Add(toiletObject);
        }
    }


}
