using UnityEngine;

namespace DishevelledBadger.FlashFrostVale.Player
{
    public class PopulationPanel : ResourcePanel
    {
        private float current = 0;
        private float max = 0;

        public void SetValue(float value)
        {
            current = value;
            textMesh.text = Mathf.Floor(current).ToString() + "/" + Mathf.Floor(max).ToString();
        }

        public void SetMaxValue(float value)
        {
            max = value;
            textMesh.text = Mathf.Floor(current).ToString() + "/" + Mathf.Floor(max).ToString();
        }
    }
}