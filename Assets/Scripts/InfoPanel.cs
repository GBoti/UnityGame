using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InfoPanel : MonoBehaviour
{
    [SerializeField]
    private ResourcePanel foodPanel;
    [SerializeField]
    private ResourcePanel woodPanel;
    [SerializeField]
    private ResourcePanel mudPanel;
    [SerializeField]
    private ResourcePanel stonePanel;
    [SerializeField]
    private TextMeshProUGUI textMesh;

    public void Show(TriangleHex h)
    {
        textMesh.text = h.Terrain;

        foodPanel.SetValue(h.Resources["food"]);
        woodPanel.SetValue(h.Resources["wood"]);
        mudPanel.SetValue(h.Resources["mud"]);
        stonePanel.SetValue(h.Resources["stone"]);

        transform.gameObject.SetActive(true);
    }

    public void Hide()
    {
        transform.gameObject.SetActive(false);
    }
}
