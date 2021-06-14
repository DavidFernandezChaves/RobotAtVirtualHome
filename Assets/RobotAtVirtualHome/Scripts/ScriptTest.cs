using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScriptTest : MonoBehaviour
{

    public float angle = 0;
    public List<Transform> transforms;

    private void OnDrawGizmos() {

        List<Vector3> points = new List<Vector3>();

        foreach(Transform t in transforms) {
            Gizmos.DrawSphere(Quaternion.Euler(0, -angle, 0) * t.position, 0.1f);
        }


    }
}
