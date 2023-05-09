using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Collider : MonoBehaviour
{
    private void OnMouseDown()
    {
        transform.parent.transform.GetComponent<TriangleHex>().Clicked();
        //transform.parent.transform.parent.transform.GetComponent<HexGridLayout>().
    }
}
