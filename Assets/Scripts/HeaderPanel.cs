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
        foodPanel.SetValue(colony.storage["food"], colony.production["food"]);
        woodPanel.SetValue(colony.storage["wood"], colony.production["wood"]);
        mudPanel.SetValue(colony.storage["mud"], colony.production["mud"]);
        stonePanel.SetValue(colony.storage["stone"], colony.production["stone"]);
    }
}
