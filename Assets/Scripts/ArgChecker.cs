using UnityEngine;
using System; // Required for Environment
using DishevelledBadger.FlashFrostVale.Globals;

public class ArgChecker : MonoBehaviour
{
    void Start()
    {
        if (DebugManager.DebugModeEnabled)
        {
            string[] args = Environment.GetCommandLineArgs();
            Debug.Log("--- Command Line Arguments ---");
            foreach (string arg in args)
            {
                Debug.Log(arg);
            }
            Debug.Log("----------------------------");
        }
    }
}