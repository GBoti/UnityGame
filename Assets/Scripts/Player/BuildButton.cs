using UnityEngine;
using UnityEngine.UI;

namespace DishevelledBadger.FlashFrostVale.Player
{
    // This represented one building in the scrollable list
    // When this was pressed the building got placed
    // in the future this should send the [Command] to the server
    // to initiate building placement
    public class BuildButton : MonoBehaviour
    {
        public GameObject buildingTypes;

        void Start()
        {
            Button button = GetComponent<Button>();
            button.onClick.AddListener(() =>
            {
                buildingTypes.SetActive(true);
            });
        }
    }
}
