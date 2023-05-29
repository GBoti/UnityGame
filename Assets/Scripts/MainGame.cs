using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MainGame : MonoBehaviour
{
    private float timePassed = 0f;
    public HexGridLayout map;
    public Colony colony;
    public TextMeshProUGUI message;
    public CameraController cameraController;

    private void Start()
    {
        StartGame();
    }

    private void StartGame()
    {
        message.gameObject.SetActive(false);
        colony.InitColony();
        map.DisplayBoard();
    }

    private void Update()
    {
        timePassed += Time.deltaTime;
        if (timePassed > 1f)
        {
            colony.Produce();
            timePassed = 0f;
        }
        if (colony.Structures.Find(s => s.buildingName == "Monument"))
        {
            message.text = "Victory";
            message.gameObject.SetActive(true);
        }
    }
}
