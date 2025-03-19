using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BuildingButton : MonoBehaviour
{
    public Building building;
    public HexGridLayout hexGrid;
    public GameObject buildingTypes;
    public Colony colony;

    void Start()
    {
        Button button = GetComponent<Button>();
        button.onClick.AddListener(() =>
        {
            colony.AddBuilding(hexGrid.CurrentSelected, building);
            buildingTypes.SetActive(false);
            hexGrid.infoPanel.Show(hexGrid.CurrentSelected);
        });
    }
}
