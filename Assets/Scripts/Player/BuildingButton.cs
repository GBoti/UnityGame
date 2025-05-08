using UnityEngine;
using UnityEngine.UI;
using DishevelledBadger.FlashFrostVale.Server;

namespace DishevelledBadger.FlashFrostVale.Player
{
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
}
