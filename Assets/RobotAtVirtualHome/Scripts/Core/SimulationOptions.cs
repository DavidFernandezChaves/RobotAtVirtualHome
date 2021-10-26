using System;
using System.Collections.Generic;
using UnityEditor;
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

        [Tooltip("Sun rotation during the simulation. Modify the ambient light of the simulation.")]
        [Range(0,360)]
        public float SunRotation = 100;

        [Tooltip("Set the status of the roof lights")]
        public LightStatus StateGeneralLight;
        [Tooltip("Set the status of the lamps")]
        public LightStatus StateLights;
        [Tooltip("Set the status of the doors")]
        public DoorStatus RandomStateDoor;

        [Header("Customization")]
        [Tooltip("Set up a set of materials with which the walls of each type of room will be painted.")]
        public List<PairForMaterials> WallsMaterialsByRoomType;
        [Tooltip("Set up a set of materials with which the floors of each type of room will be painted.")]
        public List<PairForMaterials> FloorsMaterialsByRoomType;

        [SerializeField]
        [Tooltip("Choose an object model according to the object type. Use the <= 0 to choose it randomly.")]
        public List<PairForSeed> specifySeedByObjectType;

        [Header("Startup Intances")]
        [Tooltip("Insert the prefab of the robots you want to load at the start of the simulation")]
        public GameObject userPrefab;
        [Tooltip("Insert the prefab of the robots you want to load at the start of the simulation")]
        public List<Agent> agentsToInstantiate;

        [Header("Replicity")]
        [Tooltip("You can replicate a previous simulation by inserting the generated \"EnviromentLog\" file in the SimulationsLog folder and then dragging it here.")]
        public TextAsset simulationLog;

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
        public List<Material> materials;
    }

    [Serializable]
    public struct Agent
    {
        public string name;
        public string ip;
        public GameObject prefab;
        public Agent(string name, string ip, string root)
        {
            this.name = name;
            this.ip = ip;
            this.prefab = (GameObject)AssetDatabase.LoadAssetAtPath(root, typeof(GameObject));
        }
    }

}
