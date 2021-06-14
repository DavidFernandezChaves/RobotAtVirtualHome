using UnityEngine;
using g3;
using System.Collections.Generic;

public class Example : MonoBehaviour {
    // Just for the demo I used Transforms so I can simply move them around in the scene
    public Transform[] transforms;
    public Example ejemplo;

    public Box3d box;

    private void OnDrawGizmos() {
        // First wehave to convert the Unity Vector3 array
        // into the g3 type g3.Vector3d

        var listapuntos = new List<Vector3d>();

        foreach(Transform t in transforms) {
            listapuntos.Add(t.position);
        }

        if(ejemplo != null) {
            foreach (Transform t in ejemplo.transforms) {
                listapuntos.Add(t.position);
            }

        }

        // BOOM MAGIC!!!
        var orientedBoundingBox = new ContOrientedBox3(listapuntos.ToArray());

        box = orientedBoundingBox.Box;

        // Now just convert the information back to Unity Vector3 positions and axis
        // Since g3.Vector3d uses doubles but Unity Vector3 uses floats
        // we have to explicitly cast to Vector3
        var center = (Vector3)box.Center;

        var axisX = (Vector3)box.AxisX;
        var axisY = (Vector3)box.AxisY;
        var axisZ = (Vector3)box.AxisZ;
        var extends = (Vector3)box.Extent;

        Debug.Log(extends);

        // Now we can simply calculate our 8 vertices of the bounding box
        var A = center - extends.z * axisZ - extends.x * axisX - axisY * extends.y;
        var B = center - extends.z * axisZ + extends.x * axisX - axisY * extends.y;
        var C = center - extends.z * axisZ + extends.x * axisX + axisY * extends.y;
        var D = center - extends.z * axisZ - extends.x * axisX + axisY * extends.y;

        var E = center + extends.z * axisZ - extends.x * axisX - axisY * extends.y;
        var F = center + extends.z * axisZ + extends.x * axisX - axisY * extends.y;
        var G = center + extends.z * axisZ + extends.x * axisX + axisY * extends.y;
        var H = center + extends.z * axisZ - extends.x * axisX + axisY * extends.y;

        // And finally visualize it
        Gizmos.DrawLine(A, B);
        Gizmos.DrawLine(B, C);
        Gizmos.DrawLine(C, D);
        Gizmos.DrawLine(D, A);

        Gizmos.DrawLine(E, F);
        Gizmos.DrawLine(F, G);
        Gizmos.DrawLine(G, H);
        Gizmos.DrawLine(H, E);

        Gizmos.DrawLine(A, E);
        Gizmos.DrawLine(B, F);
        Gizmos.DrawLine(D, H);
        Gizmos.DrawLine(C, G);

        Gizmos.DrawSphere((Vector3)box.Corner(0), 0.1f);

        //And Here we ca just be amazed;)
    }
}