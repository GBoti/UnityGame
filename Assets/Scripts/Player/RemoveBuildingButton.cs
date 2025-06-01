using UnityEngine;
using UnityEngine.UI;
using DishevelledBadger.FlashFrostVale.Server;

namespace DishevelledBadger.FlashFrostVale.Player
{
    // This class did the logic for removing a building form a tile
    // Right now it does nothing
    // In the multiplayer context it will send a [Command] to the server
    // to remove the building from this players colony
    public class RemoveBuildingButton : MonoBehaviour
    {
        public HexGridLayout hexGrid;
        public Colony colony;

        void Start()
        {
            Button button = GetComponent<Button>();
            button.onClick.AddListener(() =>
            {

            });
        }
    }
}
