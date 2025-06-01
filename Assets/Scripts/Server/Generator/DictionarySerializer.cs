using System;
using System.Collections.Generic;
using UnityEngine;

namespace DishevelledBadger.FlashFrostVale.Server.Generator
{
    // MonoBehaviour to hold and initialize a serialized dictionary of map features.
    // Used on a GameObject in the scene to configure features from the Unity editor.
    public class DictionarySerializer : MonoBehaviour
    {
        [SerializeField]
        string thisObjName; // Optional name for identifying this serializer instance, primarily for debugging.

        [SerializeField]
        NewDict newDict; // The custom serializable representation of the dictionary, configured in Inspector.

        Dictionary<AbstractGenerator, int> objectsNames; // Runtime dictionary of features and their counts.

        private void Start()
        {
            // On start, convert the serializable 'newDict' into a standard runtime Dictionary.
            if (newDict != null)
            {
                objectsNames = newDict.ToDictionary();
            }
        }
    }

    // Custom serializable class to represent a dictionary in the Unity Inspector.
    // Unity doesn't directly serialize Dictionaries with complex keys like AbstractGenerator.
    [Serializable]
    public class NewDict
    {
        [SerializeField]
        NewDictItem[] thisDictItems; // An array of key-value pairs, editable in the Inspector.

        // Converts the array of NewDictItem into a standard C# Dictionary.
        public Dictionary<AbstractGenerator, int> ToDictionary()
        {
            Dictionary<AbstractGenerator, int> runtimeDict = new Dictionary<AbstractGenerator, int>();
            if (thisDictItems != null)
            {
                foreach (var item in thisDictItems)
                {
                    // Ensure the feature is assigned and not already added (though Dictionary.Add would throw).
                    if (item != null && item.feature != null && !runtimeDict.ContainsKey(item.feature))
                    {
                        runtimeDict.Add(item.feature, item.generationNumber);
                    }
                }
            }
            return runtimeDict;
        }
    }

    // Represents a single key-value pair for the NewDict class.
    // Each item links an AbstractGenerator (a map feature) to an integer (its generation count).
    [Serializable]
    public class NewDictItem
    {
        [SerializeField]
        public AbstractGenerator feature; // The map feature generator (e.g., Tarn, MountainRange).

        [SerializeField]
        public int generationNumber; // How many times this feature should be generated.
    }
}
