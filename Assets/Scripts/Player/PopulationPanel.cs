using UnityEngine;

namespace DishevelledBadger.FlashFrostVale.Player
{
    // Right now unsude, in the future when/if population is introduced into the game
    // this will be able to signal that current/max population are in a place
    // e.g., in a building there are 2/3 population wokring, or there are 
    // 1/10 population in your colony that doesn't have a job
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