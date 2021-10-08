using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RobotAtVirtualHome {
    public class Door : MonoBehaviour {

        public List<GameObject> doors;
        public bool m_state;

        public void SetDoor(bool state) {
            m_state = state;
            doors.ForEach(g => g.SetActive(state));
        }

        private void Start()
        {
            SetDoor(m_state);
        }
    }
}