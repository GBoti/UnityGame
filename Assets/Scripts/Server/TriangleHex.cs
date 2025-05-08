using System.Collections.Generic;
using UnityEngine;

namespace DishevelledBadger.FlashFrostVale.Server
{
    public enum side
    {
        left,
        topleft,
        topright,
        right,
        bottomright,
        bottomleft
    }

    public class TriangleHex : MonoBehaviour
    {
        private Vector2Int indexCoordinates;
        private Dictionary<side, TriangleHex> neighbours;
        private Material basic;
        private Material selected;
        private Material backGround;
        private string terrain;
        private Dictionary<string, float> resources;

        private float height;
        private Building occupant;
        private Vector3 previousLocalScale;

        public Vector2Int IndexCoordinates
        {
            set => indexCoordinates = value;
            get => indexCoordinates;
        }

        public string Terrain
        {
            set => terrain = value;
            get => terrain;
        }

        public Material Basic
        {
            set => basic = value;
            get => basic;
        }

        public float Height
        {
            set => height = value;
            get => height;
        }

        public Dictionary<side, TriangleHex> Neighbours
        {
            get => neighbours;
        }

        public Dictionary<string, float> Resources
        {
            get => resources;
            set => resources = value;
        }

        public Building Occupant
        {
            get => occupant;
            set
            {
                if (value == null)
                {
                    Destroy(occupant.gameObject);
                    occupant = null;
                }
                else
                {
                    occupant = Instantiate(value, transform.localPosition, Quaternion.identity);
                    occupant.transform.localScale += new Vector3(0, height, 0);
                    occupant.name = value.name;
                }
            }
        }

        public void InitiateHex(
            Vector2Int iC,
            Material b,
            Material s,
            Material bG,
            Material g,
            Material w
        )
        {
            indexCoordinates = iC;
            neighbours = new Dictionary<side, TriangleHex>();
            basic = b;
            selected = s;
            backGround = bG;
            resources = new Dictionary<string, float>();
            occupant = null;
            previousLocalScale = transform.localScale;
        }

        public void Clicked()
        {
            SetBackgroundMaterial(selected);
            //Debug.Log("Clicked hex coords: (" + transform.position.x + ", " + transform.position.z + ")\n");
        }

        public void Declicked()
        {
            SetBackgroundMaterial(backGround);
        }

        public void SetMaterial(Material m)
        {
            basic = m;
            for (int i = 0; i < transform.childCount - 1; i++)
            {
                transform.GetChild(i).GetComponent<MeshRenderer>().material = m;
            }
        }

        public void SetHeight(float h)
        {
            transform.localScale = previousLocalScale;
            transform.localScale += new Vector3(0, h * 100, 0);
        }

        public void SetBackgroundMaterial(Material mat)
        {
            transform.GetChild(7).gameObject.GetComponent<MeshRenderer>().material = mat;
        }

        public Material GetMaterial()
        {
            return transform.GetChild(0).GetComponent<MeshRenderer>().material;
        }

        public void AddNeighbour(TriangleHex nb, side s)
        {
            neighbours[s] = nb;
        }

        public void GenerateResources()
        {
            resources["food"] = 0.0f;
            resources["wood"] = 0.0f;
            resources["mud"] = 0.0f;
            resources["stone"] = 0.0f;

            switch (terrain)
            {
                case "Water":
                    resources["food"] = Random.Range(1, 3);
                    resources["wood"] = Random.Range(0, 1);
                    resources["mud"] = Random.Range(1, 2);
                    resources["stone"] = Random.Range(0, 1);
                    break;
                case "Sand":
                    resources["food"] = 0.0f;
                    resources["wood"] = 0.0f;
                    resources["mud"] = Random.Range(0, 1);
                    resources["stone"] = Random.Range(0, 1);
                    break;
                case "Meadow":
                    resources["food"] = Random.Range(1, 3);
                    resources["wood"] = Random.Range(0, 1);
                    resources["mud"] = 0.0f;
                    resources["stone"] = 0.0f;
                    break;
                case "Forest":
                    resources["food"] = Random.Range(0, 1);
                    resources["wood"] = Random.Range(2, 4);
                    resources["mud"] = 0.0f;
                    resources["stone"] = 0.0f;
                    break;
                case "Mountain":
                    resources["food"] = 0.0f;
                    resources["wood"] = 0.0f;
                    resources["mud"] = 0.0f;
                    resources["stone"] = Random.Range(1, 3);
                    break;
            }
        }
    }
}
