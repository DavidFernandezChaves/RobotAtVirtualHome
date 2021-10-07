using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RobotAtVirtualHome {
    public class House : MonoBehaviour {

        [Tooltip("The log level to use")]
        public LogLevel LogLevel = LogLevel.Normal;

        public List<Room> rooms { get; private set; }
        public Dictionary<string, VirtualObject> virtualObjects;
        public Dictionary<string,Color> semanticColors { get; private set; }
        private Transform roof;

        #region Unity Functions
        private void Awake() {
            rooms = new List<Room>();
            virtualObjects = new Dictionary<string, VirtualObject>();
            semanticColors = new Dictionary<string, Color>();
        }
        #endregion

        #region Public Functions
        public void LoadHouse(SimulationOptions simulationOptions)
        {

            for (int i = 0; i < transform.childCount; i++)
            {
                Transform editingTransform = transform.GetChild(i);
                Room room = editingTransform.GetComponent<Room>();
                if (room != null)
                {
                    rooms.Add(room);
                    int n = rooms.FindAll(r => r.roomType == room.roomType).Count;
                    editingTransform.name = room.roomType.ToString() + "_" + n;
                    room.LoadRoom(simulationOptions);
                }
                if (editingTransform.name == "Roof")
                {
                    roof = editingTransform;
                }
            }
        }

        public string RegisterVirtualObject(VirtualObject virtualObject) {
            int i = 0;
            while (virtualObjects.ContainsKey(virtualObject.tags[0].ToString() + "_" + i)) {
                i++;
            }
            var name = virtualObject.tags[0].ToString() + "_" + i;            
            virtualObjects.Add(name, virtualObject);

            Color color;
            do {
                color = new Color(Random.value, Random.value, Random.value);
            } while (semanticColors.ContainsValue(color));

            semanticColors.Add(name, color);

            return name;
        }


        public List<Room> GetRoomsType(RoomType _roomType) {
            return rooms.FindAll(r => r.roomType == _roomType);
        }

        public void SetTransparentRoof(bool mode) {

            if (roof != null) {
                if (mode) {
                    roof.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                    foreach(MeshRenderer mr in roof.GetComponentsInChildren<MeshRenderer>()) {
                        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                    }
                } else {
                    roof.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    foreach (MeshRenderer mr in roof.GetComponentsInChildren<MeshRenderer>()) {
                        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    }
                }
            }
        }
        #endregion

        #region Private Functions

        private void Log(string _msg, LogLevel lvl, bool Warning = false)
        {
            if (LogLevel <= lvl && LogLevel != LogLevel.Nothing)
            {
                if (Warning)
                {
                    Debug.LogWarning("[House]: " + _msg);
                }
                else
                {
                    Debug.Log("[House]: " + _msg);
                }
            }

        }
        #endregion


    }
}

