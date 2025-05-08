using TMPro;
using UnityEngine;
using DishevelledBadger.FlashFrostVale.Server;

namespace DishevelledBadger.FlashFrostVale.Player
{
    public class BuildingInfo : MonoBehaviour
    {
        [Header("Components")]
        public TextMeshProUGUI nameText;
        public ResourcePanel foodPanel;
        public ResourcePanel woodPanel;
        public ResourcePanel mudPanel;
        public ResourcePanel stonePanel;
        public TextMeshProUGUI descText;

        public void SetValues(Building b)
        {
            nameText.text = b.name;
            foodPanel.SetValue(b.food, null);
            woodPanel.SetValue(b.wood, null);
            mudPanel.SetValue(b.mud, null);
            stonePanel.SetValue(b.stone, null);
            descText.text = b.desc;
        }
    }
}
