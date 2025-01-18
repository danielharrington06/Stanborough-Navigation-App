using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TextLabelRendererScript : MonoBehaviour
{
    [SerializeField] private DatabaseHelperScript databaseHelper;
    [SerializeField] private UserSettingsScript userSettings;
    [SerializeField] private GameObject worldSpaceCanvas;
    [SerializeField] private GameObject textPrefab;

    private List<string[]> textLabelData;
    private bool floor;
    private List<GameObject> createdTextLabels;
    
    void Start() {
        textLabelData = new List<string[]>();
        createdTextLabels = new List<GameObject>();
        floor = userSettings.floor;
        textLabelData = databaseHelper.GetTextLabels(floor);
        GenerateNewTextLabels();
    }

    void Update() {

        if (userSettings.floor != floor) {
            floor = userSettings.floor;
            // destroy current then generate new ones for new floor
            DestroyPreviousTextLabels();
            textLabelData = databaseHelper.GetTextLabels(floor);
            GenerateNewTextLabels();
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

            // get the tmp component
            TextMeshPro textComponent = textObject.GetComponent<TextMeshPro>();
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
}
