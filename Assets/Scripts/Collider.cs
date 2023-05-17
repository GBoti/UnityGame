using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Collider : MonoBehaviour
{
    [SerializeField]
    private TriangleHex th;
    private void OnMouseDown()
    {
        GameObject.Find("HexLayout").GetComponent<HexGridLayout>().ManageSelected(
            th
        );
    }
}
