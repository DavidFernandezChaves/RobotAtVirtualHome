using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RobotAtVirtualHome {
    public class Door : MonoBehaviour {

        public List<GameObject> doors;

        public void SetDoor(bool state) {
            doors.ForEach(g => g.SetActive(state));
        }
    }
}