using TMPro;
using UnityEngine;

public class ResourcePanel : MonoBehaviour
{
    [SerializeField]
    protected TextMeshProUGUI textMesh;

    public void SetValue(float value, float? production)
    {
        if (production == null)
        {
            textMesh.text = Mathf.Round(value).ToString();
            textMesh.color = Color.white;
        }
        else
        {
            if (production >= 0)
            {
                textMesh.text = Mathf.Round(value).ToString() + " +" + Mathf.Round((float)production).ToString();
                textMesh.color = Color.white;
            }
            else
            {
                textMesh.text = Mathf.Round(value).ToString() + Mathf.Round((float)production).ToString();
                textMesh.color = Color.red;
            }
        }
    }
}