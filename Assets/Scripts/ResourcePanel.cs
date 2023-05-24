using TMPro;
using UnityEngine;

public class ResourcePanel : MonoBehaviour
{
    [SerializeField]
    protected TextMeshProUGUI textMesh;

    public void SetValue(float value)
    {
        textMesh.text = Mathf.Round(value).ToString();
    }
}