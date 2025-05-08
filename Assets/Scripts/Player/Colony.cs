using System.Collections.Generic;
using UnityEngine;
using Mirror;
using DishevelledBadger.FlashFrostVale.Server;

namespace DishevelledBadger.FlashFrostVale.Player
{
    public class Colony : NetworkBehaviour
    {
        private List<Building> structures;
        public HexGridLayout map;
        public new CameraController camera;
        public string desiredHexType;
        [Header("Main building prefab")]
        public Building mainBuilding;

        //private List<TriangleHex> territory;
        public List<Building> Structures
        {
            get => structures;
            set => structures = value;
        }

        /*public List<TriangleHex> Territory
        {
            get => territory;
            set => territory = value;
        }*/

        public Dictionary<string, float> production;
        public Dictionary<string, float> storage;

        public void PlaceColony()
        {
            List<TriangleHex> potentialBaseHexes = map.hexes.FindAll(h => h.Terrain == desiredHexType);
            if (potentialBaseHexes.Count < 1)
            {
                potentialBaseHexes = map.hexes.FindAll(h => h.Terrain == "Meadow");
                if (potentialBaseHexes.Count < 1)
                {
                    return;
                }
            }

            int index = UnityEngine.Random.Range(0, potentialBaseHexes.Count - 1);
            if (potentialBaseHexes[index].Occupant != null)
            {
                index++;
                if (index >= potentialBaseHexes.Count)
                {
                    index -= potentialBaseHexes.Count;
                }
            }
            AddBuilding(potentialBaseHexes[index], mainBuilding);
            camera.transform.position = map.GetPositionForHexFromCoordinate(
                potentialBaseHexes[index].IndexCoordinates
            );
        }

        public void InitColony()
        {
            structures = new List<Building>();

            production = new Dictionary<string, float>();
            production["food"] = 0;
            production["wood"] = 0;
            production["mud"] = 0;
            production["stone"] = 0;

            storage = new Dictionary<string, float>();
            storage["food"] = 100;
            storage["wood"] = 100;
            storage["mud"] = 0;
            storage["stone"] = 0;
        }

        public void AddBuilding(TriangleHex h, Building b)
        {
            storage["food"] -= b.foodCost;
            storage["wood"] -= b.woodCost;
            storage["mud"] -= b.mudCost;
            storage["stone"] -= b.stoneCost;
            bool sufficienResources = true;
            foreach (float n in storage.Values)
            {
                if (n < 0)
                {
                    sufficienResources = false;
                }
            }
            if (sufficienResources)
            {
                h.Occupant = b;
                structures.Add(b);
                production["food"] += b.food;
                production["wood"] += b.wood;
                production["mud"] += b.mud;
                production["stone"] += b.stone;
            }
            else
            {
                storage["food"] += b.foodCost;
                storage["wood"] += b.woodCost;
                storage["mud"] += b.mudCost;
                storage["stone"] += b.stoneCost;
            }
        }

        public void RemoveBuilding(TriangleHex h)
        {
            //RemoveTerritory(h, h.Occupant.influenceRadius);
            structures.Remove(h.Occupant);
            production["food"] -= h.Occupant.food;
            production["wood"] -= h.Occupant.wood;
            production["mud"] -= h.Occupant.mud;
            production["stone"] -= h.Occupant.stone;

            // TODO: removal cost return needs tweaking
            storage["food"] += h.Occupant.foodCost;
            storage["wood"] += h.Occupant.woodCost;
            storage["mud"] += h.Occupant.mudCost;
            storage["stone"] += h.Occupant.stoneCost;
            h.Occupant = null;
        }

        public void Produce()
        {
            storage["food"] += production["food"];
            storage["wood"] += production["wood"];
            storage["mud"] += production["mud"];
            storage["stone"] += production["stone"];
        }

        /*
        public void AddTerritory(TriangleHex center, int radius)
        {
            territory.Add(center);
            List<TriangleHex> previousWave = new List<TriangleHex>();
            previousWave.Add(center);
            List<TriangleHex> waveStore = new List<TriangleHex>();
            for (int i = 0; i < radius; i++)
            {
                foreach (TriangleHex h in previousWave)
                {
                    foreach (TriangleHex n in h.Neighbours.Values)
                    {
                        if (!territory.Contains(n))
                        {
                            territory.Add(n);
                        }
                        waveStore.Add(n);
                    }
                }

            }
        }

        public void RemoveTerritory(TriangleHex center, int radius)
        {
            //remove territory around removed building
            //check if it is in another buildings radius
        }

        public void AddBuilding(TriangleHex h, Building b)
        {
            if (structures.Count == 0)
            {
                structures.Add(b);
                AddTerritory(h, b.influenceRadius);
            }
            else
            {
                if (territory.Contains(h))
                {
                    structures.Add(b);
                    AddTerritory(h, b.influenceRadius);
                }
                else
                {
                    //Can't build here
                }
            }
        }
        */
    }
}
