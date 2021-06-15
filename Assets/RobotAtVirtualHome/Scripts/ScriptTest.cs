using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScriptTest : MonoBehaviour
{

    public float angle = 0;
    public VirtualObjectBox vob1;
    public VirtualObjectBox vob2;

    private void OnDrawGizmos() {
        if (vob1 != null && vob2 != null)
        {
            List<SemanticObject.Corner> ref1 = vob1.semanticObject.Corners;
            List<SemanticObject.Corner> obs = vob2.semanticObject.Corners;
            float distance = 0;

            for (int i = 0; i < ref1.Count; i++)
            {
                Gizmos.DrawLine(ref1[i].position, obs[i].position);
            }

            Debug.Log(VirtualObjectSystem.CalculateCornerDistance(ref1, obs, false));
            vob1 = null;
            vob2 = null;
        }

    }
}
