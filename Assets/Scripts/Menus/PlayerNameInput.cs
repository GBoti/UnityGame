using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DishevelledBadger.FlashFrostVale.Globals; // Namespace for project wide helpers like DebugManager.

// Namespace for menu-related scripts.
namespace DishevelledBadger.FlashFrostVale.Menus
{
    /// <summary>
    /// Manages the UI for player name input, including loading/saving the name via PlayerPrefs.
    /// </summary>
    public class PlayerNameInput : MonoBehaviour
    {
        [Header("UI")]
        // Input field for the player to enter their name. Assigned in Inspector.
        [SerializeField] private TMP_InputField nameInputField = null;
        // Button to confirm the name and proceed (often part of a larger UI flow). Assigned in Inspector.
        [SerializeField] private Button continueButton = null;

        // Static property to hold the player's chosen display name. Accessible globally.
        public static string DisplayName { get; private set; }

        // Key used to store and retrieve the player's name in PlayerPrefs.
        private const string PlayerNamePrefKey = "PlayerName";

        /// <summary>
        /// Called when the script instance is being loaded.
        /// Sets up the input field, loads a previously saved name (if not in test mode), and updates button interactability.
        /// </summary>
        private void Start()
        {
            SetUpInputField(); // Initialize the input field.

            // Behavior depends on whether TestMode is enabled.
            if (DebugManager.TestModeEnabled)
            {
                // In test mode, ignore PlayerPrefs for initial display; name might be auto-generated.
                nameInputField.text = ""; // Clear field for clarity.
                if (DebugManager.DebugModeEnabled)
                {
                    Debug.Log("PlayerNameInput: TestModeEnabled is TRUE. Input field not pre-filled from PlayerPrefs.");
                }
            }
            else // Not in test mode (e.g., production build).
            {
                // Load name from PlayerPrefs if it exists.
                if (PlayerPrefs.HasKey(PlayerNamePrefKey))
                {
                    DisplayName = PlayerPrefs.GetString(PlayerNamePrefKey);
                    nameInputField.text = DisplayName;
                    if (DebugManager.DebugModeEnabled)
                    {
                        Debug.Log($"PlayerNameInput: TestModeEnabled is FALSE. Input field pre-filled from PlayerPrefs: {DisplayName}");
                    }
                }
                else // No saved name found.
                {
                    DisplayName = ""; // Ensure static variable is clear.
                    nameInputField.text = "";
                    if (DebugManager.DebugModeEnabled)
                    {
                        Debug.Log("PlayerNameInput: TestModeEnabled is FALSE, but no PlayerNamePrefKey found. Input field left empty.");
                    }
                }
            }
            // Update continue button state based on whether a name is present.
            if (continueButton != null) SetPlayerName(nameInputField.text);
        }

        /// <summary>
        /// Initializes the name input field to be empty.
        /// </summary>
        private void SetUpInputField()
        {
            if (nameInputField != null) nameInputField.text = string.Empty;
        }

        /// <summary>
        /// Public method to set the player's name from external scripts if needed,
        /// and updates the continue button's interactability.
        /// Typically called by the input field's OnValueChanged event.
        /// </summary>
        /// <param name="name">The name entered by the player.</param>
        public void SetPlayerName(string name) // Renamed from original to avoid conflict if used by InputField event
        {
            if (continueButton != null) continueButton.interactable = !string.IsNullOrEmpty(name);
        }

        /// <summary>
        /// Saves the currently entered player name to PlayerPrefs (if not in test mode)
        /// and updates the static DisplayName property.
        /// Provides a fallback name if the input is empty.
        /// </summary>
        public void SavePlayerName()
        {
            string finalName = nameInputField.text;
            // If the input field is empty, use a default name.
            if (string.IsNullOrEmpty(finalName))
            {
                finalName = "UnnamedPlayer";
            }
            DisplayName = finalName; // Update the static property.

            if (DebugManager.TestModeEnabled)
            {
                // In test mode, might not save to PlayerPrefs or save an empty string to ensure auto-generation.
                PlayerPrefs.SetString(PlayerNamePrefKey, "");
            }
            else // Not in test mode.
            {
                PlayerPrefs.SetString(PlayerNamePrefKey, DisplayName); // Save the name.
                // PlayerPrefs.Save(); // Explicitly save PlayerPrefs if needed, though Unity often saves on quit/focus loss.
                if (DebugManager.DebugModeEnabled)
                    Debug.Log($"PlayerNameInput: Name saved to PlayerPrefs and static DisplayName: {DisplayName}");
            }
        }
    }
}
