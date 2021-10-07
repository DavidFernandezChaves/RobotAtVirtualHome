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
        public List<Light> generalLights { get; private set; }
        [SerializeField]
        public List<Door> doors { get; private set; }
        [SerializeField]
        public List<Light> lamps { get; private set; }

        #region Unity Functions
        private void Awake() {

            generalLights = new List<Light>();
            lamps = new List<Light>();
            doors = new List<Door>();
        }

        public void LoadRoom(SimulationOptions simulationOptions) {
            //If you are using Vimantic architecture
            gameObject.AddComponent<SemanticRoom>();

            SendMessage("SetRoomID", transform.name,SendMessageOptions.DontRequireReceiver);
            SendMessage("SetRoomType", roomType.ToString(),SendMessageOptions.DontRequireReceiver);


            for (int i = 0; i < transform.childCount; i++)
            {
                Transform editingTransform = transform.GetChild(i);

                //General Light Case
                Light light = editingTransform.GetComponent<Light>();
                if (light != null)
                {
                    generalLights.Add(light);
                    editingTransform.name = "Light_" + generalLights.Count();
                }
            }

            lamps = GetComponentsInChildren<Light>(true).ToList();
            generalLights.ForEach(l => lamps.Remove(l));
            doors = GetComponentsInChildren<Door>().ToList();

            TurnLights(simulationOptions.StateGeneralLight, generalLights);
            TurnLights(simulationOptions.StateLights, lamps);
            SetDoors(simulationOptions.RandomStateDoor);

            List<Material> materialsWall = new List<Material>();
            foreach (PairForMaterials pair in simulationOptions.WallsMaterials.FindAll(pair => pair.roomType == roomType)) {
                materialsWall.Add(pair.material);
            }

            if (materialsWall.Count == 0)
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

            List<Material> materialsFloor = new List<Material>();
            foreach (PairForMaterials pair in simulationOptions.FloorsMaterials.FindAll(pair => pair.roomType == roomType))
            {
                materialsFloor.Add(pair.material);
            }

            if (materialsFloor.Count == 0)
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
                        d.SetDoor(false);
                        break;
                    case DoorStatus.Radomly:
                        d.SetDoor(Random.value >= 0.5f);
                        break;
                }
            }
        }

        public void TurnLights(LightStatus state, List<Light> lights) {
            foreach(Light l in lights)
            {
                switch (state)
                {
                    case LightStatus.On:
                        l.enabled = true;
                        break;
                    case LightStatus.Off:
                        l.enabled = false;
                        break;
                    case LightStatus.Radomly:
                        l.enabled = Random.value >= 0.5f;
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
        }

        public void PaintFloor(Material paint) {
            var mats = GetComponent<MeshRenderer>().materials;
            if (mats[0].name.ToLower().Contains("floor")) {
                mats[0] = paint;
            } else {
                mats[1] = paint;
            }
            GetComponent<MeshRenderer>().materials = mats;
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

