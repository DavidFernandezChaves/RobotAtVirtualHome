using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RobotAtVirtualHome {

    public class Room : MonoBehaviour {
        [Header("General")]
        [Tooltip("The log level to use")]
        public LogLevel LogLevel = LogLevel.Normal;

        [Tooltip("Room type")]
        public RoomType roomType;

        [SerializeField]
        public List<Door> doors { get; private set; }

        [SerializeField]
        public Material wallMaterial { get; private set; }
        [SerializeField]
        public Material floorMaterial { get; private set; }


        #region Unity Functions
        private void Awake() {
            doors = new List<Door>();
        }

        public void LoadRoom(SimulationOptions simulationOptions) {
            //If you are using Vimantic architecture
            gameObject.AddComponent<SemanticRoom>();

            SendMessage("SetRoomID", transform.name,SendMessageOptions.DontRequireReceiver);
            SendMessage("SetRoomType", roomType.ToString(),SendMessageOptions.DontRequireReceiver);


            doors = GetComponentsInChildren<Door>().ToList();

            SetDoors(simulationOptions.RandomStateDoor);

            List<Material> materialsWall = simulationOptions.WallsMaterialsByRoomType.Find(pair => pair.roomType == roomType).materials;
            if (materialsWall == null || materialsWall.Count == 0)
            {
                materialsWall = Resources.LoadAll("Walls", typeof(Material)).Cast<Material>().ToList();
            }

            if (materialsWall.Count != 0)
            {
                PaintWall(materialsWall[Random.Range(0, materialsWall.Count)]);
            }
            else
            {
                Log("No wall painting found", LogLevel.Error, true);
            }

            List<Material> materialsFloor = simulationOptions.FloorsMaterialsByRoomType.Find(pair => pair.roomType == roomType).materials;
            if (materialsFloor == null || materialsFloor.Count == 0)
            {
                materialsFloor = Resources.LoadAll("Floors", typeof(Material)).Cast<Material>().ToList();
            }

            if (materialsFloor.Count != 0)
            {
                PaintFloor(materialsFloor[Random.Range(0, materialsFloor.Count)]);
            }
            else
            {
                Log("No floor painting found", LogLevel.Error, true);
            }

        }
        #endregion

        #region Public Functions
        public void SetDoors(DoorStatus status) {
            foreach(Door d in doors)
            {
                switch (status)
                {
                    case DoorStatus.Open:
                        d.SetDoor(false);
                        break;
                    case DoorStatus.Close:
                        d.SetDoor(true);
                        break;
                    case DoorStatus.Radomly:
                        d.SetDoor(Random.value >= 0.5f);
                        break;
                }
            }
        }

        public void PaintWall(Material paint) {
            var mats = GetComponent<MeshRenderer>().materials;
            if (mats[0].name.ToLower().Contains("wall")) {
                mats[0] = paint;
            } else {
                mats[1] = paint;
            }            
            GetComponent<MeshRenderer>().materials = mats;
            wallMaterial = paint;
        }

        public void PaintFloor(Material paint) {
            var mats = GetComponent<MeshRenderer>().materials;
            if (mats[0].name.ToLower().Contains("floor")) {
                mats[0] = paint;
            } else {
                mats[1] = paint;
            }
            GetComponent<MeshRenderer>().materials = mats;
            floorMaterial = paint;
        }
        #endregion

        #region Private Functions
        private void Log(string _msg, LogLevel lvl, bool Warning=false) {
            if (LogLevel <= lvl && LogLevel != LogLevel.Nothing)
            {
                if (Warning)
                {
                    Debug.LogWarning("[Room " + roomType.ToString() + "]: " + _msg);
                }
                else
                {
                    Debug.Log("[Room " + roomType.ToString() + "]: " + _msg);
                }
            }
                
        }
        #endregion
    }
}

