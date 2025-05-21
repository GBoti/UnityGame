using UnityEngine;
using Mirror;

namespace DishevelledBadger.FlashFrostVale.Networking
{
    public class PlayerListSync : NetworkBehaviour
    {
        public readonly SyncList<LobbyPlayerData> SyncedLobbyPlayers = new SyncList<LobbyPlayerData>();

        public static PlayerListSync Instance;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
        }
    }

    public struct LobbyPlayerData
    {
        public string DisplayName;
        public bool IsReady;
    }

}