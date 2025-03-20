using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [Header("Perlin noise offset scale")]
    [SerializeField]
    public float scaler = 0.2f;

    [Header("Map parameters")]
    [SerializeField]
    public Vector2Int gridSize;

    public List<TriangleHex> hexes;

    [SerializeField]
    public Material ground;

    [SerializeField]
    public Material water;

    [SerializeField]
    public Material forest;

    [SerializeField]
    public Material sand;

    [SerializeField]
    public Material mountain;

    [SerializeField]
    public Material mountainPeak;

    [Header("Generator parameters")]
    [SerializeField]
    NewDict featuresDict;

    Dictionary<AbstractGenerator, int> features;

    [SerializeField]
    public float heightWeight;

    [SerializeField]
    public float pathWeight;

    //Procedural generation
    //PerlinNoise -> height
    //Pathfinding from one side to the other from weights of the tiles
    //After that the hexes get material based on height and neighbours
    public void Procedural_Map_Generate()
    {
        float perlinNoiseOffsetX = UnityEngine.Random.Range(0, 1000);
        float perlinNoiseOffsetY = UnityEngine.Random.Range(0, 1000);
        foreach (TriangleHex h in hexes)
        {
            Vector2 pos = h.IndexCoordinates;
            h.Height = (int)(
                Mathf.PerlinNoise(
                    (pos.x + perlinNoiseOffsetX) * scaler,
                    (pos.y + perlinNoiseOffsetY) * scaler
                ) * 10
            );
        }

        features = featuresDict.ToDictionary();
        foreach (AbstractGenerator f in features.Keys)
        {
            for (int i = 0; i < features[f]; i++)
            {
                f.Generate(hexes, gridSize);
            }
        }

        foreach (TriangleHex h in hexes)
        {
            switch (h.Height)
            {
                case -1.0f:
                    h.SetMaterial(water);
                    h.Terrain = "Water";
                    h.Height = 7.0f;
                    break;
                case 0.1f:
                    h.SetMaterial(water);
                    h.Terrain = "Water";
                    break;
                case 0.5f:
                    h.SetMaterial(sand);
                    h.Terrain = "Sand";
                    break;
                case < 4:
                    h.SetMaterial(ground);
                    h.Terrain = "Meadow";
                    break;
                case < 7:
                    h.SetMaterial(forest);
                    h.Terrain = "Forest";
                    break;
                case < 9:
                    h.SetMaterial(mountain);
                    h.Terrain = "Mountain";
                    break;
                default:
                    h.SetMaterial(mountainPeak);
                    h.Terrain = "Snowy Peak";
                    break;
            }
            h.GenerateResources();
            h.SetHeight(h.Height);
        }
    }

    public void RunGenerator(IGeneratorInterface feature)
    {
        feature.Generate(hexes, gridSize);
    }
}
