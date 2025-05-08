using UnityEngine;
using UnityEngine.UI;
using DishevelledBadger.FlashFrostVale.Server;

namespace DishevelledBadger.FlashFrostVale.Player
{
    public class RemoveBuildingButton : MonoBehaviour
    {
        public HexGridLayout hexGrid; // this needs change, the building removal should be requested by the player and then executed by the server
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
}
