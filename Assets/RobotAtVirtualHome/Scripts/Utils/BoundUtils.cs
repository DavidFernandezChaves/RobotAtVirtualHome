using UnityEngine;
using System.Collections;

namespace RobotAtVirtualHome
{
    public static class BoundUtils
    {

        // Gets an axis aligned bound box around an array of game objects
        public static Bounds GetBounds(Transform[] objs)
        {
            if (objs == null || objs.Length == 0)
            {
                return new Bounds(Vector3.zero, Vector3.zero);
            }

            float minX = Mathf.Infinity;
            float maxX = -Mathf.Infinity;
            float minY = Mathf.Infinity;
            float maxY = -Mathf.Infinity;
            float minZ = Mathf.Infinity;
            float maxZ = -Mathf.Infinity;

            Vector3[] points = new Vector3[8];

            foreach (Transform go in objs)
            {
                GetBoundsPointsNoAlloc(go, points);
                foreach (Vector3 v in points)
                {
                    if (v.x < minX) minX = v.x;
                    if (v.x > maxX) maxX = v.x;
                    if (v.y < minY) minY = v.y;
                    if (v.y > maxY) maxY = v.y;
                    if (v.z < minZ) minZ = v.z;
                    if (v.z > maxZ) maxZ = v.z;
                }
            }

            float sizeX = maxX - minX;
            float sizeY = maxY - minY;
            float sizeZ = maxZ - minZ;

            Vector3 center = new Vector3(minX + sizeX / 2.0f, minY + sizeY / 2.0f, minZ + sizeZ / 2.0f);

            return new Bounds(center, new Vector3(sizeX, sizeY, sizeZ));
        }

        // Pass in a game object and a Vector3[8], and the corners of the mesh.bounds in 
        //   in world space are returned in the passed array;
        public static void GetBoundsPointsNoAlloc(Transform go, Vector3[] points)
        {
            if (points == null || points.Length < 8)
            {
                Debug.Log("Bad Array");
                return;
            }
            MeshFilter mf = go.GetComponent<MeshFilter>();
            if (mf == null)
            {
                for (int i = 0; i < points.Length; i++)
                    points[i] = go.transform.position;
                return;
            }

            Transform tr = go.transform;

            Vector3 v3Center = mf.mesh.bounds.center;
            Vector3 v3ext = mf.mesh.bounds.extents;

            points[0] = tr.TransformPoint(new Vector3(v3Center.x - v3ext.x, v3Center.y + v3ext.y, v3Center.z - v3ext.z));  // Front top left corner
            points[1] = tr.TransformPoint(new Vector3(v3Center.x + v3ext.x, v3Center.y + v3ext.y, v3Center.z - v3ext.z));  // Front top right corner
            points[2] = tr.TransformPoint(new Vector3(v3Center.x - v3ext.x, v3Center.y - v3ext.y, v3Center.z - v3ext.z));  // Front bottom left corner
            points[3] = tr.TransformPoint(new Vector3(v3Center.x + v3ext.x, v3Center.y - v3ext.y, v3Center.z - v3ext.z));  // Front bottom right corner
            points[4] = tr.TransformPoint(new Vector3(v3Center.x - v3ext.x, v3Center.y + v3ext.y, v3Center.z + v3ext.z));  // Back top left corner
            points[5] = tr.TransformPoint(new Vector3(v3Center.x + v3ext.x, v3Center.y + v3ext.y, v3Center.z + v3ext.z));  // Back top right corner
            points[6] = tr.TransformPoint(new Vector3(v3Center.x - v3ext.x, v3Center.y - v3ext.y, v3Center.z + v3ext.z));  // Back bottom left corner
            points[7] = tr.TransformPoint(new Vector3(v3Center.x + v3ext.x, v3Center.y - v3ext.y, v3Center.z + v3ext.z));  // Back bottom right corner
        }
    }
}