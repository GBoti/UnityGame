using System.Collections.Generic;
using TMPro;
using UnityEngine;
using DishevelledBadger.FlashFrostVale.Player;

namespace DishevelledBadger.FlashFrostVale.Server
{
    public class MainGame : MonoBehaviour
    {
        private float timePassed = 0f;
        public HexGridLayout map;
        public List<Colony> colonies;
        public TextMeshProUGUI message;
        public CameraController cameraController;
        private bool victory = false;

        private void Start()
        {
            StartGame();
        }

        private void StartGame()
        {
            message.gameObject.SetActive(false);
            foreach (Colony c in colonies)
            {
                c.PlaceColony();
                c.InitColony();
            }
            map.DisplayBoard();
        }

        private void Update()
        {
            if (!victory)
            {
                timePassed += Time.deltaTime;
                if (timePassed > 1f)
                {
                    foreach (Colony c in colonies)
                    {
                        c.Produce();
                    }
                    timePassed = 0f;
                }
                foreach (Colony c in colonies)
                {
                    if (c.Structures.Find(s => s.buildingName == "Monument"))
                    {
                        message.text = "Victory";
                        message.gameObject.SetActive(true);
                        victory = true;
                    }
                }
            }
        }
    }
}
