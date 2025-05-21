using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DishevelledBadger.FlashFrostVale.Menus
{
    public class PlayerNameInput : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TMP_InputField nameInputField = null;
        [SerializeField] private Button continueButton = null;

        public static string DisplayName { get; private set; }

        private const string PlayerNamePrefKey = "PlayerName";

        private void Start()
        {
            SetUpInputField();
            if (PlayerPrefs.HasKey(PlayerNamePrefKey))
            {
                DisplayName = PlayerPrefs.GetString(PlayerNamePrefKey);
                nameInputField.text = DisplayName;
            }
            
            SetPlayerName(nameInputField.text);
        }

        private void SetUpInputField()
        {
            nameInputField.text = string.Empty;
        }

        public void SetPlayerName(string name)
        {
            continueButton.interactable = !string.IsNullOrEmpty(name);
        }

        public void SavePlayerName()
        {
            string finalName = nameInputField.text;
            if (string.IsNullOrEmpty(finalName))
            {
                finalName = "UnnamedPlayer"; // Fallback if name is empty
            }
            DisplayName = finalName;
            PlayerPrefs.SetString(PlayerNamePrefKey, DisplayName);
            Debug.Log($"PlayerNameInput: Name saved to PlayerPrefs and static DisplayName: {DisplayName}");
        }
    }
}
