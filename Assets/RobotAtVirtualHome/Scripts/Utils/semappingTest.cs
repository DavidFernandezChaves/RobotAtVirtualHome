using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

public class semappingTest : MonoBehaviour
{
    [Serializable]
    public struct detection
    {
        public float exist_certainty;
        public string name;
        public float oriented_box_rot;
        public Vector3[] oriented_box;
        public Vector3 oriented_box_cen;
    }

    public TextAsset resultFile;    
    public List<detection> detections;
    public float limit;
    public bool checkAgain = false;

    private void Update()
    {
        if (checkAgain)
        {
            checkAgain = false;
            Start();
        }
    }

    // Start is called before the first frame update
    public void Start()
    {
        detections = new List<detection>();
        if (resultFile != null)
        {
            using (StringReader sr = new StringReader(resultFile.text))
            {
                string line;
                string[] values;
                detection det = new detection();
                while ((line = sr.ReadLine()) != null)
                {
                    if (line[0].Equals('-'))
                    {
                        if(det.name != "") {
                            detections.Add(det);
                        }                       

                        det = new detection();
                        line = " " + line.Substring(1);
                    }
                    Debug.Log(line);
                    values = line.Split(' ');
                    switch (values[2])
                    {
                        case "name:":
                            det.name = values[3][0].ToString().ToUpper() + values[3].Substring(1);
                            break;
                        case "exist_certainty:":
                            det.exist_certainty = float.Parse(values[3], CultureInfo.InvariantCulture);
                            break;
                        case "oriented_box_rot:":
                            det.oriented_box_rot = float.Parse(values[3], CultureInfo.InvariantCulture);
                            break;
                        case "oriented_box_cen:":
                            det.oriented_box_cen = new Vector3(float.Parse(values[3].Substring(6), CultureInfo.InvariantCulture),1f, float.Parse(values[4].Replace(")", ""), CultureInfo.InvariantCulture));
                            break;
                        case "oriented_box:":
                            var v1 = values[3].Substring(6);
                            var v2 = values[4].Replace(")", "");
                            det.oriented_box = new Vector3[4];
                            det.oriented_box[0] = new Vector3(float.Parse(values[3].Substring(9), CultureInfo.InvariantCulture), 1f, float.Parse(values[4].Split(',')[0], CultureInfo.InvariantCulture));
                            det.oriented_box[1] = new Vector3(float.Parse(values[4].Split(',')[1], CultureInfo.InvariantCulture), 1f, float.Parse(values[5].Split(',')[0], CultureInfo.InvariantCulture));
                            det.oriented_box[2] = new Vector3(float.Parse(values[5].Split(',')[1], CultureInfo.InvariantCulture), 1f, float.Parse(values[6].Split(',')[0], CultureInfo.InvariantCulture));
                            det.oriented_box[3] = new Vector3(float.Parse(values[6].Split(',')[1], CultureInfo.InvariantCulture), 1f, float.Parse(values[7].Split(',')[0], CultureInfo.InvariantCulture));
                            break;
                    }
                }

            }
        }
    }

    private void OnDrawGizmos()
    {
        if (detections != null && detections.Count > 0)
        {
            Gizmos.color = Color.red;
            foreach (detection d in detections)
            {
                if (d.exist_certainty > limit)
                {
                    Gizmos.DrawSphere(d.oriented_box_cen, 0.15f);
                    Vector3 prev = Vector3.zero;
                    foreach(Vector3 p in d.oriented_box)
                    {
                        if(prev != Vector3.zero)
                        {
                            Gizmos.DrawLine(prev, p);                            
                        }
                        prev = p;
                    }
                    Gizmos.DrawLine(prev, d.oriented_box[0]);
                }
            }
        }
    }
}
