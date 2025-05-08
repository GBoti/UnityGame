using UnityEngine;
using UnityEngine.EventSystems;

namespace DishevelledBadger.FlashFrostVale.Server
{
    public class Collider : MonoBehaviour
    {
        [SerializeField]
        private TriangleHex th;

        private void OnMouseDown()
        {
            if (!EventSystem.current.IsPointerOverGameObject())
            {
                GameObject.Find("HexLayout").GetComponent<HexGridLayout>().ManageSelected(th);
            }
        }
    }
}
