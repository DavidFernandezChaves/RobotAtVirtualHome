using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace RobotAtVirtualHome
{
    [CreateAssetMenu(menuName = "GameData/AppareanceOptions", order = 1)]
    public class SimulationOptions : ScriptableObject
    {


        [Tooltip("House to be loaded. 0 for random.")]
        [Range(0, 30)]
        public int houseSelected = 0;

        [Tooltip("Path where you want to save the collected data.")]
        public string path = @"C:\";

        [Tooltip("Set the status of the roof lights")]
        public LightStatus StateGeneralLight;
        [Tooltip("Set the status of the lamps")]
        public LightStatus StateLights;
        [Tooltip("Set the status of the doors")]
        public DoorStatus RandomStateDoor;

        [Header("Materials")]
        [Tooltip("Set up a set of materials with which the walls of each type of room will be painted.")]
        public List<PairForMaterials> WallsMaterials;
        [Tooltip("Set up a set of materials with which the floors of each type of room will be painted.")]
        public List<PairForMaterials> FloorsMaterials;

        [SerializeField]
        [Tooltip("Choose an object model according to the object type. Use the <= 0 to choose it randomly.")]
        public List<PairForSeed> ObjectsSeed;
    }

    [Serializable]
    public struct PairForSeed
    {
        public ObjectTag objectTag;
        public int seed;
    }

    [Serializable]
    public struct PairForMaterials
    {
        public RoomType roomType;
        public Material material;
    }

}
