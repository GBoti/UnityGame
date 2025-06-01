using TMPro;
using UnityEngine;

namespace DishevelledBadger.FlashFrostVale.Player
{
    // Manages the display of a single resource value and its production rate on a UI panel.
    public class ResourcePanel : MonoBehaviour
    {
        [SerializeField] // Assignable in the Unity Inspector.
        protected TextMeshProUGUI textMesh; // The TextMeshPro component to display the resource info.

        /// <summary>
        /// Updates the displayed resource value and production rate.
        /// </summary>
        /// <param name="value">The current amount of the resource.</param>
        /// <param name="production">The per-turn production rate (nullable). If null, only value is shown.</param>
        public void SetValue(float value, float? production)
        {
            if (textMesh == null) return; // Guard clause if textMesh is not assigned.

            if (production >= 0) // Positive or zero production.
            {
                // Display current value and positive production (e.g., "100 +5").
                textMesh.text =
                    Mathf.Round(value).ToString()
                    + " +"
                    + Mathf.Round((float)production).ToString();
                textMesh.color = Color.white; // Default text color.
            }
            else // Negative production (consumption).
            {
                // Display current value and negative production (e.g., "100 -2").
                // Mathf.Round((float)production) will include the minus sign.
                textMesh.text =
                    Mathf.Round(value).ToString()
                    + " " // Added parenthesis for clarity
                    + Mathf.Round((float)production).ToString();
                textMesh.color = Color.red; // Red text color to indicate a deficit.
            }
        }
    }
}