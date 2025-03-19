using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InfoPanel : MonoBehaviour
{
    [SerializeField]
    private ResourcePanel foodPanel;

    [SerializeField]
    private ResourcePanel woodPanel;

    [SerializeField]
    private ResourcePanel mudPanel;

    [SerializeField]
    private ResourcePanel stonePanel;

    [SerializeField]
    private TextMeshProUGUI textMesh;

    [SerializeField]
    private BuildButton buildButton;

    [SerializeField]
    private BuildingInfo buildingInfoPanel;

    [SerializeField]
    private RemoveBuildingButton removeButton;
    private TriangleHex selected;

    public void Show(TriangleHex h)
    {
        selected = null;
        buildButton.transform.gameObject.SetActive(false);
        buildingInfoPanel.transform.gameObject.SetActive(false);
        removeButton.transform.gameObject.SetActive(false);

        selected = h;

        textMesh.text = h.Terrain;

        float food = h.Resources["food"];
        float wood = h.Resources["wood"];
        float mud = h.Resources["mud"];
        float stone = h.Resources["stone"];

        if (h.Occupant != null)
        {
            buildingInfoPanel.SetValues(h.Occupant);
            buildingInfoPanel.transform.gameObject.SetActive(true);
            removeButton.transform.gameObject.SetActive(true);
        }
        else
        {
            buildButton.transform.gameObject.SetActive(true);
        }

        foodPanel.SetValue(food, null);
        woodPanel.SetValue(wood, null);
        mudPanel.SetValue(mud, null);
        stonePanel.SetValue(stone, null);

        transform.gameObject.SetActive(true);
    }

    public void Hide()
    {
        selected = null;
        transform.gameObject.SetActive(false);
    }
}
