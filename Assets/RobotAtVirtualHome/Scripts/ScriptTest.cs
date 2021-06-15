using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ScriptTest : MonoBehaviour
{
    public VirtualObjectBox vob1;
    public VirtualObjectBox vob2;
    public bool Mide=false;
    public bool Ordena = false;

    private void OnDrawGizmos() {
        if (vob1 != null && vob2 != null)
        {
            List<SemanticObject.Corner> ref1 = vob1.semanticObject.Corners;
            List<SemanticObject.Corner> obs = vob2.semanticObject.Corners;

            for (int i = 0; i < ref1.Count; i++)
            {
                Gizmos.DrawLine(ref1[i].position, obs[i].position);
            }

            if (Mide) {
                Debug.Log(VirtualObjectSystem.CalculateCornerDistance(ref1, obs, false));
                Mide = false;
            }

            if (Ordena) {
                vob2.semanticObject.SetNewCorners(YNN(vob1.semanticObject.Corners, vob2.semanticObject.Corners));
                Debug.Log(VirtualObjectSystem.CalculateCornerDistance(ref1, obs, false));
                Ordena = false;
            }

        }

    }

    static public List<SemanticObject.Corner> YNN(List<SemanticObject.Corner> reference, List<SemanticObject.Corner> observation) {

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
