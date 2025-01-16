using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TextLabelRendererScript : MonoBehaviour
{
    [SerializeField] private DatabaseHelperScript databaseHelper;
    [SerializeField] private UserSettingsScript userSettings;

    private List<string[]> textLabelData;
    private bool floor;
    private List<GameObject> createdTextLabels;
    public GameObject textPrefab;

    void Start() {
        textLabelData = new List<string[]>();
        createdTextLabels = new List<GameObject>();
        floor = userSettings.floor;
    }

    void Update() {

        if (userSettings.floor != floor) {
            floor = userSettings.floor;
            // destroy current then generate new ones for new floor
            DestroyPreviousTextLabels();
            databaseHelper.GetTextLabels(floor);
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

            // adjust position
            textObject.transform.position = new Vector3(Convert.ToSingle(textLabelData[i][1]), Convert.ToSingle(textLabelData[i][2]), 0);

            // Set the text content
            TextMeshPro textComponent = textObject.GetComponent<TextMeshPro>();
            if (textComponent != null)
            {
                textComponent.text = textLabelData[i][0];
            }

            // add to the list of created labels
            createdTextLabels.Add(textObject);
        }
        
    }


}
