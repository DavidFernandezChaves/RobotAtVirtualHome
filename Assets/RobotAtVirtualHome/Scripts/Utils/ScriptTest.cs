using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ViMantic;
using RobotAtVirtualHome;
using System;

public class ScriptTest : MonoBehaviour
{
    public GameObject groundtruth;
    public GameObject detection;

    public float distanceToMeasure;

    public VirtualObjectBox vob1;
    public VirtualObjectBox vob2;
    public bool Mide = false;
    public bool Ordena = false;

    private Vector3 refPose;

    private void OnDrawGizmos()
    {
        if (vob1 != null && vob2 != null)
        {
            List<SemanticObject.Corner> ref1 = vob1.semanticObject.Corners;
            List<SemanticObject.Corner> obs = vob2.semanticObject.Corners;

            for (int i = 0; i < ref1.Count; i++)
            {
                Gizmos.DrawLine(ref1[i].position, obs[i].position);
            }

            if (Mide)
            {
                Debug.Log(VirtualObjectSystem.CalculateMatchingScore(ref1, obs));
                Mide = false;
            }

            if (Ordena)
            {
                vob2.semanticObject.SetNewCorners(YNN(vob1.semanticObject.Corners, vob2.semanticObject.Corners));
                Debug.Log(VirtualObjectSystem.CalculateMatchingScore(ref1, obs));
                Ordena = false;
            }
        }

        if(groundtruth != null)
        {

            var gtRotation = groundtruth.transform.rotation;
            var gtPosition = groundtruth.transform.position;
            var boundcenter = BoundUtils.GetBounds(groundtruth.GetComponentsInChildren<Transform>()).center;
            var desplazamiento = gtPosition - boundcenter;
            groundtruth.transform.rotation = Quaternion.Euler(0,0,0);
            groundtruth.transform.position = Vector3.zero;            

            var bound = BoundUtils.GetBounds(groundtruth.GetComponentsInChildren<Transform>());
            Gizmos.DrawWireCube(bound.center, bound.size);
            

            List<Vector3> corners = new List<Vector3>();

            corners.Add(detection.GetComponentInChildren<VirtualObjectBox>().semanticObject.Corners[0].position);
            corners.Add(detection.GetComponentInChildren<VirtualObjectBox>().semanticObject.Corners[1].position);
            corners.Add(detection.GetComponentInChildren<VirtualObjectBox>().semanticObject.Corners[2].position);
            corners.Add(detection.GetComponentInChildren<VirtualObjectBox>().semanticObject.Corners[3].position);

            for(int i = 0; i<corners.Count;i++)
            {
                corners[i] -= gtPosition;
                corners[i] = RotatePointAroundPivot(corners[i], Vector3.zero, Quaternion.Inverse(gtRotation));
            }

            Vector3 c2 = detection.GetComponentInChildren<VirtualObjectBox>().semanticObject.Corners[2].position;
            Vector3 c6 = detection.GetComponentInChildren<VirtualObjectBox>().semanticObject.Corners[6].position;
            Vector3 c0 = detection.GetComponentInChildren<VirtualObjectBox>().semanticObject.Corners[0].position;
            Vector3 c1 = detection.GetComponentInChildren<VirtualObjectBox>().semanticObject.Corners[1].position;
            Vector3 c3 = detection.GetComponentInChildren<VirtualObjectBox>().semanticObject.Corners[3].position;
            Vector3 c4 = detection.GetComponentInChildren<VirtualObjectBox>().semanticObject.Corners[4].position;
            Vector3 c5 = detection.GetComponentInChildren<VirtualObjectBox>().semanticObject.Corners[5].position;
            Vector3 c7 = detection.GetComponentInChildren<VirtualObjectBox>().semanticObject.Corners[7].position;

            Vector3 center = detection.GetComponentInChildren<VirtualObjectBox>().semanticObject.Position;
            Gizmos.color = Color.gray;
            Gizmos.DrawSphere(c2, 0.1f);
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(c6, 0.1f);
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(c2, c6);

            c2 -= gtPosition;
            c6 -= gtPosition;
            c0 -= gtPosition;
            c1 -= gtPosition;
            c3 -= gtPosition;
            c4 -= gtPosition;
            c5 -= gtPosition;
            c7 -= gtPosition;


            c2 = RotatePointAroundPivot(c2, Vector3.zero, Quaternion.Inverse(gtRotation));
            c6 = RotatePointAroundPivot(c6, Vector3.zero, Quaternion.Inverse(gtRotation));
            c0 = RotatePointAroundPivot(c0, Vector3.zero, Quaternion.Inverse(gtRotation));
            c1 = RotatePointAroundPivot(c1, Vector3.zero, Quaternion.Inverse(gtRotation));
            c3 = RotatePointAroundPivot(c3, Vector3.zero, Quaternion.Inverse(gtRotation));
            c4 = RotatePointAroundPivot(c4, Vector3.zero, Quaternion.Inverse(gtRotation));
            c5 = RotatePointAroundPivot(c5, Vector3.zero, Quaternion.Inverse(gtRotation));
            c7 = RotatePointAroundPivot(c7, Vector3.zero, Quaternion.Inverse(gtRotation));

            Gizmos.color = Color.gray;
            Gizmos.DrawSphere(c0, 0.1f);
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(c3, 0.1f);
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(c1, 0.1f);
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(c0, c1);

            
            Gizmos.DrawSphere(c0, 0.05f);
            Gizmos.DrawSphere(c1, 0.05f);
            Gizmos.DrawSphere(c2, 0.05f);
            Gizmos.DrawSphere(c3, 0.05f);
            Gizmos.DrawSphere(c4, 0.05f);
            Gizmos.DrawSphere(c5, 0.05f);
            Gizmos.DrawSphere(c6, 0.05f);
            Gizmos.DrawSphere(c7, 0.05f);

            groundtruth.transform.rotation = gtRotation;
            groundtruth.transform.position = gtPosition;

            if (Mide)
            {
                Mide = false;
                StartCoroutine(mide(corners,bound));                
            }
            Gizmos.DrawSphere(refPose, 0.01f);
            //groundtruth.transform.rotation = gtRotation;
        }

    }

    public static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Quaternion rotation)
    {
        return rotation * (point - pivot) + pivot;
    }

    private IEnumerator mide(List<Vector3> c, Bounds bound)
    {
        float minZ = c[0].z;
        float maxZ = c[0].z;

        for (int i=1;i<c.Count;i++){ 
            if(c[i].z < minZ)
            {
                minZ = c[i].z;
            }
            if (c[i].z > maxZ)
            {
                maxZ = c[i].z;
            }
        }

        float inside = 0;
        float total = 0;
        float angle = Mathf.Atan2(c[3].y-c[0].y, c[3].x - c[0].x);
        float x,y;

        for (float d = 0; d <= Vector3.Distance(c[0], c[3]); d += distanceToMeasure) {
            for (float d2 = 0; d2 <= Vector3.Distance(c[0], c[1]); d2 += distanceToMeasure)
            {
                x = c[0].x + d2*Mathf.Cos(angle-Mathf.PI/2) + d * Mathf.Cos(angle);
                y = c[0].y + d2 * Mathf.Sin(angle - Mathf.PI / 2) + d * Mathf.Sin(angle);
                for (float z = minZ; z <= maxZ; z += distanceToMeasure)
                {
                    if (bound.Contains(new Vector3(x, y, z)))
                    {
                        inside++;
                    }
                    total++;
                    //refPose = new Vector3(x, y, z);                    
                    //yield return null;
                }
            }
        }
        float union =(total - inside) * Mathf.Pow(distanceToMeasure, 3)+bound.size.x* bound.size.y* bound.size.z;
        Debug.Log("IoU: " + inside*Mathf.Pow(distanceToMeasure,3)/union);
        yield return null;
    }

    static public List<SemanticObject.Corner> YNN(List<SemanticObject.Corner> reference, List<SemanticObject.Corner> observation)
    {

        Queue<SemanticObject.Corner> top = new Queue<SemanticObject.Corner>();
        top.Enqueue(observation[2]);
        top.Enqueue(observation[5]);
        top.Enqueue(observation[4]);
        top.Enqueue(observation[7]);
        Queue<SemanticObject.Corner> bottom = new Queue<SemanticObject.Corner>();
        bottom.Enqueue(observation[0]);
        bottom.Enqueue(observation[3]);
        bottom.Enqueue(observation[6]);
        bottom.Enqueue(observation[1]);


        top.Enqueue(top.Dequeue());
        bottom.Enqueue(bottom.Dequeue());


        List<SemanticObject.Corner> result = new List<SemanticObject.Corner> {
            bottom.ElementAt(0),
            bottom.ElementAt(3),
            top.ElementAt(0),
            bottom.ElementAt(1),
            top.ElementAt(2),
            top.ElementAt(1),
            bottom.ElementAt(2),
            top.ElementAt(3)
        };

        return result;
    }
}