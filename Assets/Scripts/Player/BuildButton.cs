using UnityEngine;
using UnityEngine.UI;

namespace DishevelledBadger.FlashFrostVale.Player
{
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
