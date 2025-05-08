using UnityEngine;

namespace DishevelledBadger.FlashFrostVale.Server
{
    public class BuildingStencil : MonoBehaviour
    {
        public Building building;
        public string buildingName;
        public string buildingDescription;

        private void Start()
        {
            building = null;
            buildingName = "";
            buildingDescription = "";
        }

        public void InitBuildingStencil(Building b, string n, string d)
        {
            building = b;
            buildingName = n;
            buildingDescription = d;
        }
    }
}
