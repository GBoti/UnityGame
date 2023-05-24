using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeaderPanel : MonoBehaviour
{
    public ResourcePanel foodPanel;
    public ResourcePanel woodPanel;
    public ResourcePanel mudPanel;
    public ResourcePanel stonePanel;
    public Colony colony;

    private void Update()
    {
        foodPanel.SetValue(colony.storage["food"]);
        woodPanel.SetValue(colony.storage["wood"]);
        mudPanel.SetValue(colony.storage["mud"]);
        stonePanel.SetValue(colony.storage["stone"]);
    }
}
