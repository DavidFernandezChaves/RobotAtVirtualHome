using UnityEngine;
using g3;

public class Example : MonoBehaviour {
    // Just for the demo I used Transforms so I can simply move them around in the scene
    public Transform[] transforms;

    private void OnDrawGizmos() {
        // First wehave to convert the Unity Vector3 array
        // into the g3 type g3.Vector3d
        var points3d = new Vector3d[transforms.Length];
        for (var i = 0; i < transforms.Length; i++) {
            // Thanks to the g3 library implictely casted from UnityEngine.Vector3 to g3.Vector3d
            points3d[i] = transforms[i].position;
        }

        // BOOM MAGIC!!!
        var orientedBoundingBox = new ContOrientedBox3(points3d);

        // Now just convert the information back to Unity Vector3 positions and axis
        // Since g3.Vector3d uses doubles but Unity Vector3 uses floats
        // we have to explicitly cast to Vector3
        var center = (Vector3)orientedBoundingBox.Box.Center;

        var axisX = (Vector3)orientedBoundingBox.Box.AxisX;
        var axisY = (Vector3)orientedBoundingBox.Box.AxisY;
        var axisZ = (Vector3)orientedBoundingBox.Box.AxisZ;
        var extends = (Vector3)orientedBoundingBox.Box.Extent;

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

        //And Here we ca just be amazed;)
    }
}