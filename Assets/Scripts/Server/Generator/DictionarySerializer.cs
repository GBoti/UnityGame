using System;
using System.Collections.Generic;
using UnityEngine;

namespace DishevelledBadger.FlashFrostVale.Server.Generator
{
    public class DictionarySerializer : MonoBehaviour
    {
        [SerializeField]
        string thisObjName;

        [SerializeField]
        NewDict newDict;

        Dictionary<AbstractGenerator, int> objectsNames;

        private void Start()
        {
            objectsNames = newDict.ToDictionary();
        }
    }

    [Serializable]
    public class NewDict
    {
        [SerializeField]
        NewDictItem[] thisDictItems;

        public Dictionary<AbstractGenerator, int> ToDictionary()
        {
            Dictionary<AbstractGenerator, int> newDict = new Dictionary<AbstractGenerator, int>();

            foreach (var item in thisDictItems)
            {
                newDict.Add(item.feature, item.generationNumber);
            }

            return newDict;
        }
    }

    [Serializable]
    public class NewDictItem
    {
        [SerializeField]
        public AbstractGenerator feature;

        [SerializeField]
        public int generationNumber;
    }
}
