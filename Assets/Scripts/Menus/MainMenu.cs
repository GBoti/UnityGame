using UnityEngine;
using DishevelledBadger.FlashFrostVale.Networking;

namespace DishevelledBadger.FlashFrostVale.Menus
{
    public class MainMenu : MonoBehaviour
    {
        [SerializeField] private NetworkManagerFFV networkManager = null;

        [Header("UI")]
        [SerializeField] private GameObject landingPagePanel = null;
        [SerializeField] private GameObject lobbyPagePanel = null;

        public void HostLobby()
        {
            networkManager.StartHost();

            landingPagePanel.SetActive(false);
            lobbyPagePanel.SetActive(true);
        }
    }
}
