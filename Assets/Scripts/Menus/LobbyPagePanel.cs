using UnityEngine;
using Mirror;
using DishevelledBadger.FlashFrostVale.Networking;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using DishevelledBadger.FlashFrostVale.Globals;

namespace DishevelledBadger.FlashFrostVale.Menus
{
    public class LobbyPagePanel : MonoBehaviour
    {
        public static LobbyPagePanel Instance { get; private set; }

        [Header("References")]
        [SerializeField] private NetworkManagerFFV networkManagerFFV;
        [SerializeField] private GameObject playerEntryPrefab;
        [SerializeField] private Transform playerListParent;

        [SerializeField] private Button readyButton;
        [SerializeField] private Button hostStartGameButton;

        private Dictionary<NetworkLobbyPlayerFFV, GameObject> playerUIEntries = new Dictionary<NetworkLobbyPlayerFFV, GameObject>();
        private NetworkLobbyPlayerFFV localLobbyPlayer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                if (DebugManager.DebugModeEnabled)
                    Debug.LogWarning("LobbyPagePanel: Duplicate instance detected, destroying self.");
                return;
            }
            Instance = this;
            if (DebugManager.DebugModeEnabled)
                Debug.Log("LobbyPagePanel: Instance set in Awake.");

            if (networkManagerFFV == null)
            {
                networkManagerFFV = NetworkManager.singleton as NetworkManagerFFV;
                if (networkManagerFFV == null)
                {
                    if (DebugManager.DebugModeEnabled)
                        Debug.LogError("LobbyPagePanel: NetworkManagerFFV not found in scene!");
                }
            }

            if (readyButton != null)
            {
                readyButton.onClick.AddListener(OnReadyButtonClicked);
            }
            if (hostStartGameButton != null)
            {
                hostStartGameButton.onClick.AddListener(OnHostStartGameButtonClicked);
                hostStartGameButton.gameObject.SetActive(false);
            }
            if (DebugManager.DebugModeEnabled)
                Debug.Log("LobbyPagePanel: Awake complete.");
        }

        private void OnEnable()
        {
            if (DebugManager.DebugModeEnabled)
                Debug.Log("LobbyPagePanel: OnEnable called.");
            ClearAllPlayerEntries();
            RefreshAllPlayerEntries();
        }

        private void OnDisable()
        {
            if (DebugManager.DebugModeEnabled)
                Debug.Log("LobbyPagePanel: OnDisable called. Clearing entries.");
            ClearAllPlayerEntries();
        }

        public void AddPlayerEntry(NetworkLobbyPlayerFFV player)
        {
            if (playerUIEntries.ContainsKey(player))
            {
                if (DebugManager.DebugModeEnabled)
                    Debug.LogWarning($"LobbyPagePanel.AddPlayerEntry: Player {player.DisplayName} already has a UI entry. Updating existing.");
                UpdatePlayerEntryUI(player);
                return;
            }

            if (playerEntryPrefab == null)
            {
                if (DebugManager.DebugModeEnabled)
                    Debug.LogError("LobbyPagePanel.AddPlayerEntry: playerEntryPrefab is null! Cannot create UI entry.");
                return;
            }
            if (playerListParent == null)
            {
                if (DebugManager.DebugModeEnabled)
                    Debug.LogError("LobbyPagePanel.AddPlayerEntry: playerListParent is null! Cannot parent UI entry.");
                return;
            }

            GameObject playerEntryGO = Instantiate(playerEntryPrefab, playerListParent);
            playerUIEntries.Add(player, playerEntryGO);
            if (DebugManager.DebugModeEnabled)
                Debug.Log($"LobbyPagePanel.AddPlayerEntry: Created UI entry for player: {player.DisplayName} (IsLocal: {player.isLocalPlayer}, IsLeader: {player.IsLeader})");

            if (player.isLocalPlayer)
            {
                localLobbyPlayer = player;
                UpdateReadyButtonText();
                if (DebugManager.DebugModeEnabled)
                    Debug.Log($"LobbyPagePanel.AddPlayerEntry: Local player identified: {player.DisplayName}.");
            }

            UpdatePlayerEntryUI(player);

            if (player.isLocalPlayer && player.IsLeader && NetworkServer.active)
            {
                hostStartGameButton.gameObject.SetActive(true);
                if (DebugManager.DebugModeEnabled)
                    Debug.Log($"LobbyPagePanel.AddPlayerEntry: Activating Host Start Game Button for {player.DisplayName}.");
            }
            else if (hostStartGameButton != null)
            {
                hostStartGameButton.gameObject.SetActive(false);
            }
        }

        public void RemovePlayerEntry(NetworkLobbyPlayerFFV player)
        {
            if (playerUIEntries.TryGetValue(player, out GameObject entryToRemove))
            {
                Destroy(entryToRemove);
                playerUIEntries.Remove(player);
                if (DebugManager.DebugModeEnabled)
                    Debug.Log($"LobbyPagePanel.RemovePlayerEntry: Removed UI entry for player: {player.DisplayName}.");
            }
            else
            {
                if (DebugManager.DebugModeEnabled)
                    Debug.LogWarning($"LobbyPagePanel.RemovePlayerEntry: Attempted to remove UI for player {player.DisplayName} but no entry found.");
            }

            if (player.isLocalPlayer && player.IsLeader && !NetworkServer.active)
            {
                if (hostStartGameButton != null) hostStartGameButton.gameObject.SetActive(false);
                if (DebugManager.DebugModeEnabled)
                    Debug.Log($"LobbyPagePanel.RemovePlayerEntry: Hiding Host Start Game Button (leader disconnected).");
            }
            UpdateHostStartButtonInteractable();
        }

        public void UpdatePlayerEntryUI(NetworkLobbyPlayerFFV player)
        {
            if (playerUIEntries.TryGetValue(player, out GameObject playerEntryGO))
            {
                TMP_Text nameText = playerEntryGO.transform.Find("NameText")?.GetComponent<TMP_Text>();
                TMP_Text readyText = playerEntryGO.transform.Find("ReadyText")?.GetComponent<TMP_Text>();
                Image backgroundImage = playerEntryGO.GetComponent<Image>();

                if (nameText == null && DebugManager.DebugModeEnabled)
                    Debug.LogError($"LobbyPagePanel.UpdatePlayerEntryUI: 'NameText' child not found in playerEntryPrefab for {player.DisplayName}!");
                if (readyText == null && DebugManager.DebugModeEnabled)
                    Debug.LogError($"LobbyPagePanel.UpdatePlayerEntryUI: 'ReadyText' child not found in playerEntryPrefab for {player.DisplayName}!");

                if (nameText != null)
                {
                    nameText.text = player.DisplayName;
                }
                if (readyText != null)
                {
                    readyText.text = player.IsReady ? "<color=green>READY</color>" : "<color=red>NOT READY</color>";
                }

                if (backgroundImage != null)
                {
                    if (player.isLocalPlayer)
                    {
                        backgroundImage.color = new Color(0.8f, 0.9f, 1.0f, 1.0f);
                    }
                    else
                    {
                        backgroundImage.color = Color.black;
                    }
                }
                if (DebugManager.DebugModeEnabled)
                    Debug.Log($"LobbyPagePanel.UpdatePlayerEntryUI: Updated UI for player: {player.DisplayName}, Ready: {player.IsReady} (on client {NetworkClient.connection?.identity.netId})");
            }
            else
            {
                if (DebugManager.DebugModeEnabled)
                    Debug.LogWarning($"LobbyPagePanel.UpdatePlayerEntryUI: No UI entry found for player {player.DisplayName}.");
            }

            if (player.isLocalPlayer)
            {
                UpdateReadyButtonText();
            }
        }

        private void OnReadyButtonClicked()
        {
            if (localLobbyPlayer != null)
            {
                if (DebugManager.DebugModeEnabled)
                    Debug.Log($"LobbyPagePanel.OnReadyButtonClicked: Sending CmdReadyUp for {localLobbyPlayer.DisplayName}.");
                localLobbyPlayer.CmdReadyUp();
            }
            else
            {
                if (DebugManager.DebugModeEnabled)
                    Debug.LogWarning("LobbyPagePanel.OnReadyButtonClicked: Local Lobby Player not found!");
            }
        }

        private void UpdateReadyButtonText()
        {
            if (localLobbyPlayer != null && readyButton != null)
            {
                TMP_Text buttonText = readyButton.GetComponentInChildren<TMP_Text>();
                if (buttonText != null)
                {
                    buttonText.text = localLobbyPlayer.IsReady ? "Cancel Ready" : "Ready Up";
                    if (DebugManager.DebugModeEnabled)
                        Debug.Log($"LobbyPagePanel.UpdateReadyButtonText: Ready button text updated to: {buttonText.text}.");
                }
            }
        }

        private void OnHostStartGameButtonClicked()
        {
            if (localLobbyPlayer != null && localLobbyPlayer.IsLeader && networkManagerFFV != null)
            {
                if (DebugManager.DebugModeEnabled)
                    Debug.Log($"LobbyPagePanel.OnHostStartGameButtonClicked: Host sending CmdStartGame for {localLobbyPlayer.DisplayName}.");
                localLobbyPlayer.CmdStartGame();
            }
            else
            {
                if (DebugManager.DebugModeEnabled)
                    Debug.LogWarning("LobbyPagePanel.OnHostStartGameButtonClicked: Conditions not met (not local leader, or manager null).");
            }
        }

        public void SetHostStartGameButtonInteractable(bool interactable)
        {
            if (hostStartGameButton != null)
            {
                hostStartGameButton.interactable = interactable;
                if (DebugManager.DebugModeEnabled)
                    Debug.Log($"LobbyPagePanel.SetHostStartGameButtonInteractable: Host Start Game button interactable set to: {interactable} (from TargetRpc).");
            }
            else
            {
                if (DebugManager.DebugModeEnabled)
                    Debug.LogWarning("LobbyPagePanel.SetHostStartGameButtonInteractable: hostStartGameButton is null!");
            }
        }

        public void UpdateHostStartButtonInteractable()
        {
            if (localLobbyPlayer != null && localLobbyPlayer.IsLeader && NetworkServer.active)
            {
                if (networkManagerFFV != null)
                {
                    bool allReady = networkManagerFFV.IsReadyToStart();
                    if (DebugManager.DebugModeEnabled)
                        Debug.Log($"LobbyPagePanel.UpdateHostStartButtonInteractable: Local check - all ready? {allReady}. (TargetRpc should handle actual state)");
                }
            }
        }

        private void ClearAllPlayerEntries()
        {
            foreach (var entry in playerUIEntries.Values)
            {
                Destroy(entry);
            }
            playerUIEntries.Clear();
            if (DebugManager.DebugModeEnabled)
                Debug.Log("LobbyPagePanel: Cleared all player UI entries.");
        }

        private void RefreshAllPlayerEntries()
        {
            ClearAllPlayerEntries();
            NetworkLobbyPlayerFFV[] players = FindObjectsByType<NetworkLobbyPlayerFFV>(FindObjectsSortMode.None);
            if (DebugManager.DebugModeEnabled)
                Debug.Log($"LobbyPagePanel: Found {players.Length} NetworkLobbyPlayerFFV objects on scene refresh.");
            foreach (var player in players)
            {
                AddPlayerEntry(player);
            }
            if (localLobbyPlayer != null && localLobbyPlayer.IsLeader && NetworkServer.active)
            {
                hostStartGameButton.gameObject.SetActive(true);
            }
            else if (hostStartGameButton != null)
            {
                hostStartGameButton.gameObject.SetActive(false);
            }
        }
    }
}
