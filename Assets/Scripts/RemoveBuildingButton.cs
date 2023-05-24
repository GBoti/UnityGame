using UnityEngine;
using UnityEngine.UI;

public class RemoveBuildingButton : MonoBehaviour
{
    public HexGridLayout hexGrid;
    public Colony colony;
    void Start()
    {
        Button button = GetComponent<Button>();
        button.onClick.AddListener(() =>
        {
            colony.RemoveBuilding(hexGrid.CurrentSelected);
            hexGrid.infoPanel.Show(hexGrid.CurrentSelected);
        });
    }
}
