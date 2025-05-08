using UnityEngine;
using UnityEngine.UI;
using DishevelledBadger.FlashFrostVale.Server;

namespace DishevelledBadger.FlashFrostVale.test
{
    public class RegenerateMapButton : MonoBehaviour
    {
        [SerializeField]
        private HexGridLayout hglayout;

        public float scalerInput = 0.2f;

        void Start()
        {
            Button button = GetComponent<Button>();
            button.onClick.AddListener(() =>
            {
                hglayout.Generator.scaler = scalerInput;
                hglayout.LayoutGrid();
                hglayout.Generator.Procedural_Map_Generate();
            });
        }

        void Update() { }

        public void getScalerInput(string input)
        {
            if (input != null)
            {
                scalerInput = float.Parse(input);
            }
        }
    }
}
