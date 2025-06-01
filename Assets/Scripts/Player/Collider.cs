using DishevelledBadger.FlashFrostVale.Globals;
using DishevelledBadger.FlashFrostVale.Networking;
using DishevelledBadger.FlashFrostVale.Server;
using Mirror;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DishevelledBadger.FlashFrostVale.Player // Namespace might be misleading if this is client-side click handling
{
    // Handles mouse click detection on a visual hex tile.
    public class Collider : MonoBehaviour
    {
        [SerializeField]
        private TriangleHex th; // Reference to the TriangleHex logic this collider represents.

        void Awake()
        {
            // If TriangleHex (th) isn't assigned in Inspector, try to get it from a parent GameObject.
            if (th == null)
            {
                th = GetComponentInParent<TriangleHex>();
                // Log error if still not found, as clicks won't work correctly.
                if (th == null && DebugManager.DebugModeEnabled)
                {
                    Debug.LogError($"Collider on {gameObject.name}: TriangleHex (th) reference missing.", gameObject);
                }
            }
        }

        // Called when the mouse button is pressed down while over this GameObject's collider.
        private void OnMouseDown()
        {
            // Ignore click if the mouse is currently over a UI element (e.g., button, panel).
            if (EventSystem.current.IsPointerOverGameObject())
            {
                if (DebugManager.DebugModeEnabled) Debug.Log("Hex OnMouseDown: UI click, ignoring hex.");
                return;
            }

            // If the TriangleHex reference is missing, cannot process the click.
            if (th == null)
            {
                if (DebugManager.DebugModeEnabled) Debug.LogError($"Hex OnMouseDown on {gameObject.name}: TriangleHex (th) is null.");
                return;
            }

            // Attempt to find the local player's NetworkGamePlayerFFV component.
            NetworkGamePlayerFFV localPlayer = null;
            if (NetworkClient.active && NetworkClient.localPlayer != null)
            {
                localPlayer = NetworkClient.localPlayer.GetComponent<NetworkGamePlayerFFV>();
            }

            if (localPlayer != null)
            {
                // If local player found, notify it about the hex click for selection management.
                if (DebugManager.DebugModeEnabled) Debug.Log($"Hex OnMouseDown: Clicked {th.IndexCoordinates}. Notifying local player.");
                localPlayer.ClientManageHexSelection(th);
            }
            else if (DebugManager.DebugModeEnabled)
            {
                // Log a warning if the local player couldn't be found.
                Debug.LogWarning($"Hex OnMouseDown on {gameObject.name}: Local NetworkGamePlayerFFV not found.");
            }
        }
    }
}