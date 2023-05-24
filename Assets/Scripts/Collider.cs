using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Collider : MonoBehaviour
{
    [SerializeField]
    private TriangleHex th;
    private void OnMouseDown()
    {
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            GameObject.Find("HexLayout").GetComponent<HexGridLayout>().ManageSelected(
                th
            );
        }
    }
}
