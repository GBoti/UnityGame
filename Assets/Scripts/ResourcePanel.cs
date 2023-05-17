using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ResourcePanel : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI textMesh;

    public void SetValue(float value)
    {
        textMesh.text = Mathf.Round(value).ToString();
    }
}
