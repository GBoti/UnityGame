using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BuildingInfo : MonoBehaviour
{
    [Header("Components")]
    public TextMeshProUGUI nameText;
    public ResourcePanel foodPanel;
    public ResourcePanel woodPanel;
    public ResourcePanel mudPanel;
    public ResourcePanel stonePanel;
    public TextMeshProUGUI descText;

    public void SetValues(Building b)
    {
        nameText.text = b.name;
        foodPanel.SetValue(b.food);
        woodPanel.SetValue(b.wood);
        mudPanel.SetValue(b.mud);
        stonePanel.SetValue(b.stone);
        descText.text = b.desc;
    }
}
