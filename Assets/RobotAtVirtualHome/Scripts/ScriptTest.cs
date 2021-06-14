using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScriptTest : MonoBehaviour
{

    public float angle = 0;
    public List<Transform> transforms;
    public List<Transform> transforms2;

    private void OnDrawGizmos() {

        List<Vector3> points = new List<Vector3>();
        float distance = 0;

        for(int i = 0; i < transforms.Count; i++)
        {
            distance += Vector3.Distance(transforms[i].position, transforms2[i].position);
            Gizmos.color = Color.white;
            Gizmos.DrawSphere(transforms[i].position, 0.1f);
            Gizmos.color = Color.white;
            Gizmos.DrawSphere(transforms2[i].position, 0.1f);
        }

        Debug.Log(distance);
    }
}
