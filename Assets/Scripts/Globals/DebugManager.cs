using UnityEngine;

// Namespace for global game utilities and settings.
namespace DishevelledBadger.FlashFrostVale.Globals
{
    /// <summary>
    /// Manages global debugging and testing flags for the application.
    /// </summary>
    public class DebugManager
    {
        // Enables or disables general debug logging throughout the application.
        public static bool DebugModeEnabled = false;

        // Enables or disables specific test mode behaviors (e.g., automatic player naming based on project path).
        public static bool TestModeEnabled = false;
    }
}
