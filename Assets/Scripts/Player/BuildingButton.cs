using UnityEngine;
using UnityEngine.UI;
using DishevelledBadger.FlashFrostVale.Server;

namespace DishevelledBadger.FlashFrostVale.Player
{
    // This class was responsible for placing a building on a hex
    // When this button is pressed the available buildings should
    // pop up e.g, in a scrollable list
    public class BuildingButton : MonoBehaviour
    {
        public Building building;
        public GameObject buildingTypes;
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
