using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RobotAtVirtualHome {
    public class House : MonoBehaviour {

        [Tooltip("The log level to use")]
        public LogLevel LogLevel = LogLevel.Normal;

        public List<Room> rooms { get; private set; }
        public List<VirtualObject> virtualObjects;
        public Dictionary<string,Color> semanticColors { get; private set; }
        private Transform roof;
        private SimulationOptions simulationOptions;

        #region Unity Functions
        private void Awake() {
            rooms = new List<Room>();
            virtualObjects = new List<VirtualObject>();
            semanticColors = new Dictionary<string, Color>();
            simulationOptions = FindObjectOfType<EnvironmentManager>().m_simulationOptions;
        }
        #endregion

        #region Public Functions
        public void LoadHouse()
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
            while (virtualObjects.Find(obj => obj.m_id == virtualObject.tags[0].ToString() + "_" + i)) {
                i++;
            }
            var name = virtualObject.tags[0].ToString() + "_" + i;            
            virtualObjects.Add(virtualObject);

            Color color;
            do {
                color = new Color(Random.value, Random.value, Random.value);
            } while (semanticColors.ContainsValue(color));

            semanticColors.Add(name, color);

            if (virtualObject.tags.Contains(ObjectTag.Lamp)|| virtualObject.tags.Contains(ObjectTag.Lighter))
            {
                bool result = false;
                switch (simulationOptions.StateLights)
                {
                    case LightStatus.On:
                        result = true;
                        break;
                    case LightStatus.Off:
                        result = false;
                        break;
                    case LightStatus.Radomly:
                        result = Random.value >= 0.5f;
                        break;
                }

                foreach (Light l in virtualObject.GetComponentsInChildren(typeof(Light), true))
                {
                    l.enabled = result;
                }
            }

            if (virtualObject.tags.Contains(ObjectTag.Light))
            {
                bool result = false;
                switch (simulationOptions.StateGeneralLight)
                {
                    case LightStatus.On:
                        result = true;
                        break;
                    case LightStatus.Off:
                        result = false;
                        break;
                    case LightStatus.Radomly:
                        result = Random.value >= 0.5f;
                        break;
                }

                foreach (Light l in virtualObject.GetComponentsInChildren(typeof(Light), true))
                {
                    l.enabled = result;
                }
            }

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

