using UnityEngine;
using Mirror;
using System;
using System.Linq;
using System.Collections.Generic;
using DishevelledBadger.FlashFrostVale.Globals;

namespace DishevelledBadger.FlashFrostVale.Networking
{
    public class NetworkManagerFFV : NetworkManager
    {
        [Scene] [SerializeField] private string menuScene = string.Empty;

        [Header("Lobby")]
        [SerializeField] private NetworkLobbyPlayerFFV lobbyPlayerPrefab = null;

        public static event Action OnClientConnectedToManager;
        public static event Action OnClientDisconnectedFromManager;

        public readonly List<NetworkLobbyPlayerFFV> LobbyPlayers = new List<NetworkLobbyPlayerFFV>();

        public override void OnStartServer()
        {
            base.OnStartServer();
            spawnPrefabs = Resources.LoadAll<GameObject>("SpawnablePrefabs").ToList();
            if (DebugManager.DebugModeEnabled)
                Debug.Log("NetworkManagerFFV: Server started.");
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            GameObject[] spawnablePrefabs = Resources.LoadAll<GameObject>("SpawnablePrefabs");
            foreach (GameObject prefab in spawnablePrefabs)
            {
                NetworkClient.RegisterPrefab(prefab);
            }
            if (DebugManager.DebugModeEnabled)
                Debug.Log("NetworkManagerFFV: Client started.");
        }

        public override void OnClientConnect()
        {
            base.OnClientConnect();
            if (DebugManager.DebugModeEnabled)
                Debug.Log("NetworkManagerFFV: Client connected to server.");
            OnClientConnectedToManager?.Invoke();
        }

        public override void OnClientDisconnect()
        {
            base.OnClientDisconnect();
            if (DebugManager.DebugModeEnabled)
                Debug.Log("NetworkManagerFFV: Client disconnected from server.");
            OnClientDisconnectedFromManager?.Invoke();
        }

        public override void OnServerConnect(NetworkConnectionToClient conn)
        {
            base.OnServerConnect(conn);
            if (numPlayers >= maxConnections)
            {
                if (DebugManager.DebugModeEnabled)
                    Debug.LogWarning($"NetworkManagerFFV: Server full. Disconnecting client {conn.connectionId}");
                conn.Disconnect();
                return;
            }
        }

        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            if (DebugManager.DebugModeEnabled)
                Debug.Log($"NetworkManagerFFV: OnServerAddPlayer called for connection {conn.connectionId}.");
            bool isLeader = LobbyPlayers.Count == 0;

            NetworkLobbyPlayerFFV lobbyPlayerInstance = Instantiate(lobbyPlayerPrefab);
            lobbyPlayerInstance.IsLeader = isLeader;

            NetworkServer.AddPlayerForConnection(conn, lobbyPlayerInstance.gameObject);
            if (DebugManager.DebugModeEnabled)
                Debug.Log($"NetworkManagerFFV: Player object spawned for connection {conn.connectionId}.");

            LobbyPlayers.Add(lobbyPlayerInstance);
            if (DebugManager.DebugModeEnabled)
                Debug.Log($"NetworkManagerFFV: Player added to server list. Total players: {LobbyPlayers.Count}.");

            NotifyPlayersOfReadyState();
        }

        [Server]
        public void NotifyPlayersOfReadyState()
        {
            if (DebugManager.DebugModeEnabled)
                Debug.Log("NetworkManagerFFV.NotifyPlayersOfReadyState: Server checking ready state...");
            bool ready = IsReadyToStart();
            NetworkLobbyPlayerFFV leaderPlayer = LobbyPlayers.FirstOrDefault(p => p.IsLeader);
            if (leaderPlayer != null)
            {
                if (DebugManager.DebugModeEnabled)
                    Debug.Log($"NetworkManagerFFV.NotifyPlayersOfReadyState: Calling TargetSetStartGameButtonInteractable on leader ({leaderPlayer.DisplayName}) with {ready}.");
                leaderPlayer.TargetSetStartGameButtonInteractable(ready);
            }
            else
            {
                if (DebugManager.DebugModeEnabled)
                    Debug.LogWarning("NetworkManagerFFV.NotifyPlayersOfReadyState: No leader player found.");
            }
        }

        [Server]
        public bool IsReadyToStart()
        {
            if (LobbyPlayers.Count == 0)
            {
                if (DebugManager.DebugModeEnabled)
                    Debug.Log("NetworkManagerFFV.IsReadyToStart: No players in lobby, not ready.");
                return false;
            }

            foreach (NetworkLobbyPlayerFFV player in LobbyPlayers)
            {
                if (string.IsNullOrWhiteSpace(player.DisplayName) || player.DisplayName == "Loading...")
                {
                    if (DebugManager.DebugModeEnabled)
                        Debug.Log($"NetworkManagerFFV.IsReadyToStart: Player {player.connectionToClient.connectionId} ('{player.DisplayName}') has no display name set yet. Not ready.");
                    return false;
                }
                if (!player.IsReady)
                {
                    if (DebugManager.DebugModeEnabled)
                        Debug.Log($"NetworkManagerFFV.IsReadyToStart: Player {player.DisplayName} is not ready. Not ready.");
                    return false;
                }
            }
            if (DebugManager.DebugModeEnabled)
                Debug.Log("NetworkManagerFFV.IsReadyToStart: All players are ready!");
            return true;
        }

        [Server]
        public void StartGame()
        {
            if (DebugManager.DebugModeEnabled)
                Debug.Log("NetworkManagerFFV.StartGame: Attempting to start game.");
            if (!IsReadyToStart())
            {
                if (DebugManager.DebugModeEnabled)
                    Debug.LogWarning("NetworkManagerFFV.StartGame: Cannot start game: Not all players are ready!");
                return;
            }
            if (DebugManager.DebugModeEnabled)
                Debug.Log("NetworkManagerFFV.StartGame: Starting game for all players!");
            // ServerChangeScene("YourGameSceneName");
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            if (DebugManager.DebugModeEnabled)
                Debug.Log($"NetworkManagerFFV.OnServerDisconnect: Connection {conn.connectionId} disconnected.");
            if (conn.identity != null)
            {
                NetworkLobbyPlayerFFV player = conn.identity.GetComponent<NetworkLobbyPlayerFFV>();
                if (player != null)
                {
                    LobbyPlayers.Remove(player);
                    if (DebugManager.DebugModeEnabled)
                        Debug.Log($"NetworkManagerFFV.OnServerDisconnect: Player {player.DisplayName} removed from server list. Total players: {LobbyPlayers.Count}.");
                }
                else
                {
                    if (DebugManager.DebugModeEnabled)
                        Debug.LogWarning($"NetworkManagerFFV.OnServerDisconnect: Disconnected connection {conn.connectionId} had no NetworkLobbyPlayerFFV identity.");
                }

                NotifyPlayersOfReadyState();
            }
            base.OnServerDisconnect(conn);
        }

        public override void OnStopServer()
        {
            LobbyPlayers.Clear();
            if (DebugManager.DebugModeEnabled)
                Debug.Log("NetworkManagerFFV: Server stopped. Lobby players cleared.");
        }
    }
}
