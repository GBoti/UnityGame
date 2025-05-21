using UnityEngine;
using Mirror;
using TMPro;
using UnityEngine.UI;
using DishevelledBadger.FlashFrostVale.Menus;
using System;
using DishevelledBadger.FlashFrostVale.Globals;

namespace DishevelledBadger.FlashFrostVale.Networking
{
    public class NetworkLobbyPlayerFFV : NetworkBehaviour
    {
        [SyncVar(hook = nameof(OnPlayerNameChanged))]
        public string DisplayName = "Loading...";
        [SyncVar(hook = nameof(OnPlayerReadyChanged))]
        public bool IsReady = false;

        private bool isLeader;

        public bool IsLeader
        {
            set
            {
                isLeader = value;
                if (DebugManager.DebugModeEnabled)
                    Debug.Log($"[{netId}] IsLeader set to {value} (isLocalPlayer: {isLocalPlayer}, isServer: {isServer})");
            }
            get { return isLeader; }
        }

        private NetworkManagerFFV lobby;

        private NetworkManagerFFV Lobby
        {
            get
            {
                if (lobby != null) { return lobby; }
                return lobby = NetworkManager.singleton as NetworkManagerFFV;
            }
        }

        private LobbyPagePanel lobbyPagePanelInstance;

        public override void OnStartClient()
        {
            base.OnStartClient();

            // --- 1. Find UI Manager (LobbyPagePanel) ---
            if (LobbyPagePanel.Instance != null)
            {
                lobbyPagePanelInstance = LobbyPagePanel.Instance;
            }
            else
            {
                lobbyPagePanelInstance = FindFirstObjectByType<LobbyPagePanel>();
                if (lobbyPagePanelInstance == null)
                {
                    if (DebugManager.DebugModeEnabled)
                        Debug.LogError($"[{netId}] NetworkLobbyPlayerFFV.OnStartClient: LobbyPagePanel not found for player {DisplayName}! UI will not update.");
                    return;
                }
            }
            if (DebugManager.DebugModeEnabled)
                Debug.Log($"[{netId}] NetworkLobbyPlayerFFV.OnStartClient: LobbyPagePanel found for {DisplayName}.");

            // --- 2. Tell UI Manager to add/update this player's entry ---
            lobbyPagePanelInstance.AddPlayerEntry(this);
            if (DebugManager.DebugModeEnabled)
                Debug.Log($"[{netId}] NetworkLobbyPlayerFFV.OnStartClient: Player entry added/updated in LobbyPagePanel for {DisplayName}.");

            // --- 3. Send Name Command (ONLY from the local player) ---
            if (isLocalPlayer)
            {
                if (DebugManager.DebugModeEnabled)
                    Debug.Log($"[{netId}] NetworkLobbyPlayerFFV.OnStartClient: This is the local player instance. Attempting to set display name.");
                string savedPlayerName = PlayerPrefs.GetString("PlayerName", "UnnamedPlayer" + UnityEngine.Random.Range(100, 999));

                if (string.IsNullOrWhiteSpace(savedPlayerName))
                {
                    savedPlayerName = "DefaultPlayer";
                    if (DebugManager.DebugModeEnabled)
                        Debug.LogWarning($"[{netId}] NetworkLobbyPlayerFFV.OnStartClient: PlayerName from PlayerPrefs was empty, defaulting to {savedPlayerName}.");
                }

                CmdSetDisplayName(savedPlayerName);
                if (DebugManager.DebugModeEnabled)
                    Debug.Log($"[{netId}] NetworkLobbyPlayerFFV.OnStartClient: CmdSetDisplayName sent with '{savedPlayerName}'.");
            }
            else
            {
                if (DebugManager.DebugModeEnabled)
                    Debug.Log($"[{netId}] NetworkLobbyPlayerFFV.OnStartClient: This is a remote player instance. Name will sync from server.");
            }
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            if (DebugManager.DebugModeEnabled)
                Debug.Log($"[{netId}] NetworkLobbyPlayerFFV.OnStopClient: Player {DisplayName} disconnected. (IsLocal: {isLocalPlayer})");
            if (lobbyPagePanelInstance != null)
            {
                lobbyPagePanelInstance.RemovePlayerEntry(this);
            }
            if (isLocalPlayer && isLeader && !NetworkServer.active)
            {
                if (lobbyPagePanelInstance != null)
                {
                    lobbyPagePanelInstance.SetHostStartGameButtonInteractable(false);
                }
            }
        }

        // --- SyncVar Hooks (Called on all clients when SyncVar changes on server) ---
        private void OnPlayerNameChanged(string oldValue, string newValue)
        {
            if (DebugManager.DebugModeEnabled)
                Debug.Log($"[{netId}] NetworkLobbyPlayerFFV.OnPlayerNameChanged: Hook fired. Old: '{oldValue}', New: '{newValue}' (IsLocal: {isLocalPlayer})");
            if (lobbyPagePanelInstance != null)
            {
                lobbyPagePanelInstance.UpdatePlayerEntryUI(this);
                if (DebugManager.DebugModeEnabled)
                    Debug.Log($"[{netId}] NetworkLobbyPlayerFFV.OnPlayerNameChanged: Requested UI update for {newValue}.");
            }
            else
            {
                if (DebugManager.DebugModeEnabled)
                    Debug.LogWarning($"[{netId}] NetworkLobbyPlayerFFV.OnPlayerNameChanged: LobbyPagePanel instance is null, cannot update UI for {newValue}.");
            }
        }

        private void OnPlayerReadyChanged(bool oldValue, bool newValue)
        {
            if (DebugManager.DebugModeEnabled)
                Debug.Log($"[{netId}] NetworkLobbyPlayerFFV.OnPlayerReadyChanged: Hook fired. Old: '{oldValue}', New: '{newValue}' (IsLocal: {isLocalPlayer})");
            if (lobbyPagePanelInstance != null)
            {
                lobbyPagePanelInstance.UpdatePlayerEntryUI(this);
                if (DebugManager.DebugModeEnabled)
                    Debug.Log($"[{netId}] NetworkLobbyPlayerFFV.OnPlayerReadyChanged: Requested UI update for {DisplayName} ready state: {newValue}.");
            }
            else
            {
                if (DebugManager.DebugModeEnabled)
                    Debug.LogWarning($"[{netId}] NetworkLobbyPlayerFFV.OnPlayerReadyChanged: LobbyPagePanel instance is null, cannot update UI for {DisplayName} ready state.");
            }

            if (isServer)
            {
                if (DebugManager.DebugModeEnabled)
                    Debug.Log($"[{netId}] NetworkLobbyPlayerFFV.OnPlayerReadyChanged: On server, notifying NetworkManagerFFV of ready state change.");
                (NetworkManager.singleton as NetworkManagerFFV)?.NotifyPlayersOfReadyState();
            }
        }

        // --- Commands (Client to Server) ---
        [Command]
        private void CmdSetDisplayName(string displayName)
        {
            if (DebugManager.DebugModeEnabled)
                Debug.Log($"[{netId}] NetworkLobbyPlayerFFV.CmdSetDisplayName: Command received on server from {connectionToClient.connectionId} (current name: {DisplayName}, new name: {displayName}).");
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = "InvalidName";
            }
            DisplayName = displayName;
            if (DebugManager.DebugModeEnabled)
                Debug.Log($"[{netId}] NetworkLobbyPlayerFFV.CmdSetDisplayName: SyncVar DisplayName set to '{DisplayName}' on server.");
        }

        [Command]
        public void CmdReadyUp()
        {
            if (DebugManager.DebugModeEnabled)
                Debug.Log($"[{netId}] NetworkLobbyPlayerFFV.CmdReadyUp: Command received on server from {DisplayName} (current ready: {IsReady}).");
            IsReady = !IsReady;
            if (DebugManager.DebugModeEnabled)
                Debug.Log($"[{netId}] NetworkLobbyPlayerFFV.CmdReadyUp: SyncVar IsReady set to {IsReady} on server.");
        }

        [Command]
        public void CmdStartGame()
        {
            if (DebugManager.DebugModeEnabled)
                Debug.Log($"[{netId}] NetworkLobbyPlayerFFV.CmdStartGame: Command received on server from {DisplayName}.");
            NetworkManagerFFV lobby = NetworkManager.singleton as NetworkManagerFFV;
            if (lobby == null)
            {
                if (DebugManager.DebugModeEnabled)
                    Debug.LogError($"[{netId}] NetworkLobbyPlayerFFV.CmdStartGame: NetworkManagerFFV is null.");
                return;
            }

            if (lobby.LobbyPlayers.Count == 0 || lobby.LobbyPlayers[0].connectionToClient != connectionToClient)
            {
                if (DebugManager.DebugModeEnabled)
                    Debug.LogWarning($"[{netId}] Player {DisplayName} (conn: {connectionToClient.connectionId}) tried to start game but is not the leader.");
                return;
            }

            if (!lobby.IsReadyToStart())
            {
                if (DebugManager.DebugModeEnabled)
                    Debug.LogWarning($"[{netId}] Cannot start game: Not all players are ready.");
                return;
            }

            lobby.StartGame();
        }


        // TargetRpc for leader's start button interactability
        [TargetRpc]
        public void TargetSetStartGameButtonInteractable(bool interactable)
        {
            if (DebugManager.DebugModeEnabled)
                Debug.Log($"[{netId}] NetworkLobbyPlayerFFV.TargetSetStartGameButtonInteractable: TargetRpc received on client for {DisplayName}. Interactable: {interactable}.");
            if (lobbyPagePanelInstance != null)
            {
                lobbyPagePanelInstance.SetHostStartGameButtonInteractable(interactable);
            }
            else
            {
                if (DebugManager.DebugModeEnabled)
                    Debug.LogError($"[{netId}] NetworkLobbyPlayerFFV.TargetSetStartGameButtonInteractable: LobbyPagePanel instance is null!");
            }
        }
    }
}