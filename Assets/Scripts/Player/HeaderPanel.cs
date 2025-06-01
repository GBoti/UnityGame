using UnityEngine;

namespace DishevelledBadger.FlashFrostVale.Player
{
    // This class will manage the panel at the top of the screen
    // It needs to request info from the server regarding the
    // players stored resources and production which it can then display
    // Did this straight from colony in the past.
    public class HeaderPanel : MonoBehaviour
    {
        public ResourcePanel foodPanel;
        public ResourcePanel woodPanel;
        public ResourcePanel mudPanel;
        public ResourcePanel stonePanel;
        public Colony colony;

        private void Update()
        {

        }
    }
}
