using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using ViMantic;

namespace RobotAtVirtualHome.Utils
{
    public class DetectionResults : MonoBehaviour
    {
        [Serializable]
        public struct ClassMatching
        {
            public string ClassInOntology;
            public ObjectTag ClassInVirtualEnvironment;
        }

        [Serializable]
        public struct DetectionMatch
        {
            public float iou;
            public float viou;
            public float distance;
        }

        public bool recordResults;

        [Header("General")]
        [Tooltip("The log level to use")]
        public LogLevel LogLevel = LogLevel.Normal;

        [SerializeField]
        [Range(0.01f,1)]
        private float m_geometricAccuracy=0.1f;
        [SerializeField]
        [Range(1, 50)]
        private int m_minDetections;
        [SerializeField]
        [Range(0.01f, 1)]
        private float m_minCertainty = 0.1f;

        [SerializeField]
        private EnvironmentManager m_enviromentManager;

        [SerializeField]
        private List<ClassMatching> m_classMatching;

        [SerializeField]
        private semappingTest m_semappingTest;
        private OntologySystem m_ontologySystem;
        private List<VirtualObject> m_detectableObjects;
        private StreamWriter writer;

        [SerializeField]
        private Dictionary<VirtualObject, DetectionMatch> detectionsProcessed_vimantic;

        [SerializeField]
        private Dictionary<VirtualObject, DetectionMatch> detectionsProcessed_semapping;



        #region Unity Functions
        private void Awake()
        {
            m_ontologySystem = FindObjectOfType<OntologySystem>();
            m_detectableObjects = new List<VirtualObject>();
        }

        private void Start()
        {
            m_enviromentManager.OnEnvironmentLoaded += GetDetectableObjects;
        }

        private void OnDestroy()
        {
            m_enviromentManager.OnEnvironmentLoaded -= GetDetectableObjects;
        }

#if UNITY_EDITOR
        //private void OnDrawGizmos()
        //{
        //    if (Application.isPlaying && this.enabled && LogLevel == LogLevel.Developer)
        //    {
        //        foreach (KeyValuePair<SemanticObject, float> dp in detectionsProcessed)
        //        {
        //            if (dp.Value < m_maxDistance)
        //            {
        //                Gizmos.color = new Color(0, 1, 0, 0.3f);
        //            }
        //            else
        //            {
        //                Gizmos.color = new Color(1,0,0,0.3f);                        
        //            }
        //            Gizmos.DrawSphere(dp.Key.Position, dp.Value);
        //        }
        //    }
        //}
#endif

        #endregion

        #region Public Functions
        public void CalculateResults()
        {
            StartCoroutine(Compare());
        }

        public float DiscreteIoU(GameObject groundtruth, List<Vector3> corners, bool volumetric)
        {
            var gtRotation = groundtruth.transform.rotation;
            var gtPosition = groundtruth.transform.position;
            groundtruth.transform.rotation = Quaternion.Euler(0, 0, 0);
            groundtruth.transform.position = Vector3.zero;
            var bound = BoundUtils.GetBounds(groundtruth.GetComponentsInChildren<Transform>());


            for (int i = 0; i < corners.Count; i++)
            {
                corners[i] -= gtPosition;
                corners[i] = RotatePointAroundPivot(corners[i], Vector3.zero, Quaternion.Inverse(gtRotation));
            }

            groundtruth.transform.rotation = gtRotation;
            groundtruth.transform.position = gtPosition;

            float minZ = Mathf.Infinity;
            float maxZ = -Mathf.Infinity;

            for (int i = 1; i < corners.Count; i++)
            {
                if (corners[i].z < minZ)
                {
                    minZ = corners[i].z;
                }
                if (corners[i].z > maxZ)
                {
                    maxZ = corners[i].z;
                }
            }
            float inside = 0;
            float total = 0;
            float angle = Mathf.Atan2(corners[3].y - corners[0].y, corners[3].x - corners[0].x);
            float x, y;

            for (float d = 0; d <= Vector3.Distance(corners[0], corners[3]); d += m_geometricAccuracy)
            {
                for (float d2 = 0; d2 <= Vector3.Distance(corners[0], corners[1]); d2 += m_geometricAccuracy)
                {
                    x = corners[0].x + d2 * Mathf.Cos(angle - Mathf.PI / 2) + d * Mathf.Cos(angle);
                    y = corners[0].y + d2 * Mathf.Sin(angle - Mathf.PI / 2) + d * Mathf.Sin(angle);
                    if (volumetric)
                    {
                        for (float z = minZ; z <= maxZ; z += m_geometricAccuracy)
                        {
                            if (bound.Contains(new Vector3(x, y, z)))
                            {
                                inside++;
                            }
                            total++;
                        }
                    }
                    else
                    {
                        if (bound.Contains(new Vector3(x, y, 0)))
                        {
                            inside++;
                        }
                        total++;
                    }
                    
                }
            }

            float IoU;
            if (volumetric)
            {
                IoU = inside * Mathf.Pow(m_geometricAccuracy, 3)
                / ((total - inside) * Mathf.Pow(m_geometricAccuracy, 3) + bound.size.x * bound.size.y * bound.size.z);
            }
            else
            {
                IoU = inside * Mathf.Pow(m_geometricAccuracy, 2)
                / ((total - inside) * Mathf.Pow(m_geometricAccuracy, 2) + bound.size.x * bound.size.y);
            }          
            return IoU;
        }

        public static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Quaternion rotation)
        {
            return rotation * (point - pivot) + pivot;
        }
        #endregion

        #region Private Functions
        private IEnumerator Compare()
        {
            detectionsProcessed_vimantic = new Dictionary<VirtualObject, DetectionMatch>();
            detectionsProcessed_semapping = new Dictionary<VirtualObject, DetectionMatch>();

            int TP_vimantic = 0, FP_vimantic = 0, FN_vimantic = 0;
            int TP_semapping = 0, FP_semapping = 0, FN_semapping = 0;

            float averageIoU_vimantic=0, averageVIoU_vimantic=0, averageDistance_vimantic = 0;
            float averageIoU_semapping=0, averageDistance_semapping = 0;

            ObjectTag objectTag;
            ClassMatching classMatching;
            VirtualObject bestMatch;
            DetectionMatch match;
            float distance, iou;
            List<Vector3> corners;


            //Calculate TP, FP and FN _vimantic
            foreach (VirtualObjectBox vob in  FindObjectsOfType(typeof(VirtualObjectBox)).Cast<VirtualObjectBox>())
            {
                SemanticObject so = vob.semanticObject;
                if (so.NDetections >= m_minDetections)
                {
                    bestMatch = null;
                    match = new DetectionMatch();

                    corners = new List<Vector3>();
                    corners.Add(so.Corners[0].position);
                    corners.Add(so.Corners[1].position);
                    corners.Add(so.Corners[2].position);
                    corners.Add(so.Corners[3].position);

                    try
                    {
                        classMatching = m_classMatching.Find(match => match.ClassInOntology.Equals(so.ObjectClass));
                        objectTag = (classMatching.ClassInOntology == null) ? (ObjectTag)Enum.Parse(typeof(ObjectTag), so.ObjectClass) : classMatching.ClassInVirtualEnvironment;

                        foreach (VirtualObject virtualObject in m_detectableObjects)
                        {
                            if (virtualObject.tags.Contains(objectTag))
                            {                                
                                distance = Vector3.Distance(BoundUtils.GetBounds(virtualObject.GetComponentsInChildren<Transform>()).center, so.Position);
                                if (distance <= 5)
                                {
                                    iou = DiscreteIoU(virtualObject.gameObject, new List<Vector3>(corners), false);                                    
                                    if (iou>0 && iou >= match.iou)
                                    {
                                        bestMatch = virtualObject;
                                        match.iou = iou;
                                        match.distance = distance;
                                    }                                    
                                }
                            }
                        }

                        if(bestMatch != null)
                        {
                            if (detectionsProcessed_vimantic.ContainsKey(bestMatch))
                            {
                                if (detectionsProcessed_vimantic[bestMatch].iou < match.iou)
                                {
                                    averageDistance_vimantic -= detectionsProcessed_vimantic[bestMatch].distance;
                                    averageDistance_vimantic += match.distance;
                                    averageIoU_vimantic -= detectionsProcessed_vimantic[bestMatch].iou;
                                    averageIoU_vimantic += match.iou;
                                    averageVIoU_vimantic -= detectionsProcessed_vimantic[bestMatch].viou;
                                    
                                    match.viou = DiscreteIoU(bestMatch.gameObject, corners, true);
                                    averageVIoU_vimantic += match.viou;
                                    detectionsProcessed_vimantic[bestMatch] = match;
                                }
                                FP_vimantic++;
                            }
                            else
                            {
                                match.viou = DiscreteIoU(bestMatch.gameObject, corners, true);
                                detectionsProcessed_vimantic.Add(bestMatch, match);
                                TP_vimantic++;
                                averageIoU_vimantic += match.iou;
                                averageVIoU_vimantic += match.viou;
                                averageDistance_vimantic += match.distance;
                            }
                        }
                        else
                        {
                            FP_vimantic ++;
                        }                       


                    }
                    catch (ArgumentException)
                    {
                        Log(so.ObjectClass + " failed to convert to ObjectTag", LogLevel.Developer);
                    }
                }

                //yield return null;
            }

            //Calculate TP, FP and FN _semapping
            foreach ( semappingTest.detection detection in m_semappingTest.detections)
            {

                if (detection.exist_certainty >= m_minCertainty)
                {
                    bestMatch = null;
                    match = new DetectionMatch();

                    corners = detection.oriented_box.ToList();
                    try
                    {
                        classMatching = m_classMatching.Find(match => match.ClassInOntology.Equals(detection.name));
                        objectTag = (classMatching.ClassInOntology == null) ? (ObjectTag)Enum.Parse(typeof(ObjectTag), detection.name) : classMatching.ClassInVirtualEnvironment;

                        foreach (VirtualObject virtualObject in m_detectableObjects)
                        {
                            if (virtualObject.tags.Contains(objectTag))
                            {
                                distance = Vector3.Distance(BoundUtils.GetBounds(virtualObject.GetComponentsInChildren<Transform>()).center, detection.oriented_box_cen);
                                if (distance <= 5)
                                {
                                    iou = DiscreteIoU(virtualObject.gameObject, corners, false);
                                    if (iou > 0 && iou >= match.iou)
                                    {
                                        bestMatch = virtualObject;
                                        match.iou = iou;
                                        match.distance = distance;
                                    }
                                }
                            }
                        }

                        if (bestMatch != null)
                        {
                            if (detectionsProcessed_semapping.ContainsKey(bestMatch))
                            {
                                if (detectionsProcessed_semapping[bestMatch].iou < match.iou)
                                {
                                    averageDistance_semapping -= detectionsProcessed_semapping[bestMatch].distance;
                                    averageDistance_semapping += match.distance;
                                    averageIoU_semapping -= detectionsProcessed_semapping[bestMatch].iou;
                                    averageIoU_semapping += match.iou;
                                    detectionsProcessed_semapping[bestMatch] = match;
                                }
                                FP_semapping++;
                            }
                            else
                            {
                                detectionsProcessed_semapping.Add(bestMatch, match);
                                TP_semapping++;
                                averageIoU_semapping += match.iou;
                                averageDistance_semapping += match.distance;
                            }
                        }
                        else
                        {
                            FP_semapping++;
                        }


                    }
                    catch (ArgumentException)
                    {
                        Log(detection.name + " failed to convert to ObjectTag", LogLevel.Developer);
                    }

                }
                yield return null;
            }

            FN_vimantic = m_detectableObjects.Count - detectionsProcessed_vimantic.Keys.Count;
            FN_semapping = m_detectableObjects.Count - detectionsProcessed_semapping.Keys.Count;

            float accuracy_vimantic = (float)TP_vimantic / (TP_vimantic + FP_vimantic);
            float recall_vimantic = (float)TP_vimantic / (TP_vimantic + FN_vimantic);
            float f1_vimantic = 2 * accuracy_vimantic * recall_vimantic / (accuracy_vimantic + recall_vimantic);
            averageIoU_vimantic /= TP_vimantic;
            averageVIoU_vimantic /= TP_vimantic;
            averageDistance_vimantic /= TP_vimantic;

            float accuracy_semapping = (float)TP_semapping / (TP_semapping + FP_semapping);
            float recall_semapping = (float)TP_semapping / (TP_semapping + FN_semapping);
            float f1_semapping = 2 * accuracy_semapping * recall_semapping / (accuracy_semapping + recall_semapping);
            averageIoU_semapping /= TP_semapping;
            averageDistance_semapping /= TP_semapping;

            Log("TP_vimantic: " + TP_vimantic.ToString(), LogLevel.Normal);
            Log("FP_vimantic: " + FP_vimantic.ToString(), LogLevel.Normal);
            Log("FN_vimantic: " + FN_vimantic.ToString(), LogLevel.Normal);
            Log("Accuracy_vimantic: " + accuracy_vimantic.ToString(), LogLevel.Normal);
            Log("Recall_vimantic: " + recall_vimantic.ToString(), LogLevel.Normal);
            Log("F1_vimantic: " + f1_vimantic.ToString("f"), LogLevel.Normal);            
            Log("Average IoU vimantic: " + averageIoU_vimantic, LogLevel.Normal);
            Log("Average VIoU vimantic: " + averageVIoU_vimantic, LogLevel.Normal);
            Log("Average Distance vimantic: " + averageDistance_vimantic.ToString(), LogLevel.Normal);


            Log("TP_semapping: " + TP_semapping.ToString(), LogLevel.Normal);
            Log("FP_semapping: " + FP_semapping.ToString(), LogLevel.Normal);
            Log("FN_semapping: " + FN_semapping.ToString(), LogLevel.Normal);
            Log("Accuracy_semapping: " + accuracy_semapping.ToString(), LogLevel.Normal);
            Log("Recall_semapping: " + recall_semapping.ToString(), LogLevel.Normal);
            Log("F1_semapping: " + f1_semapping.ToString("f"), LogLevel.Normal);            
            Log("Average IoU semapping: " + averageIoU_semapping, LogLevel.Normal);
            Log("Average Distance semapping: " + averageDistance_semapping.ToString(), LogLevel.Normal);

            if (recordResults)
            {
                writer = new StreamWriter(m_enviromentManager.path + "/Results.csv", true);
                writer.WriteLine("--------------");
                writer.WriteLine(m_enviromentManager.house.ToString() + ";Vimantic;Time;" + Time.realtimeSinceStartup.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine(m_enviromentManager.house.ToString()+ ";Vimantic;TP;" + TP_vimantic.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine(m_enviromentManager.house.ToString() + ";Vimantic;FP;" + FP_vimantic.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine(m_enviromentManager.house.ToString() + ";Vimantic;FN;" + FN_vimantic.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine(m_enviromentManager.house.ToString() + ";Vimantic;Accuracy;" + accuracy_vimantic.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine(m_enviromentManager.house.ToString() + ";Vimantic;Recall;" + recall_vimantic.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine(m_enviromentManager.house.ToString() + ";Vimantic;F1;" + f1_vimantic.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine(m_enviromentManager.house.ToString() + ";Vimantic;IoU;" + averageIoU_vimantic.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine(m_enviromentManager.house.ToString() + ";Vimantic;VIoU;" + averageVIoU_vimantic.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine(m_enviromentManager.house.ToString() + ";Vimantic;Distance;" + averageDistance_vimantic.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine("--------------");
                writer.WriteLine(m_enviromentManager.house.ToString() + ";Semapping;Time;" + Time.realtimeSinceStartup.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine(m_enviromentManager.house.ToString() + ";Semapping;TP;" + TP_semapping.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine(m_enviromentManager.house.ToString() + ";Semapping;FP;" + FP_semapping.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine(m_enviromentManager.house.ToString() + ";Semapping;FN;" + FN_semapping.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine(m_enviromentManager.house.ToString() + ";Semapping;Accuracy;" + accuracy_semapping.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine(m_enviromentManager.house.ToString() + ";Semapping;Recall;" + recall_semapping.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine(m_enviromentManager.house.ToString() + ";Semapping;F1;" + f1_semapping.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine(m_enviromentManager.house.ToString() + ";Semapping;IoU;" + averageIoU_semapping.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine(m_enviromentManager.house.ToString() + ";Semapping;Distance;" + averageDistance_semapping.ToString(CultureInfo.InvariantCulture));

                writer.Close();
            }
            

            //yield return null;
        }

        private void GetDetectableObjects()
        {
            List<VirtualObject> objectsInTheHouse = new List<VirtualObject>(m_enviromentManager.house.virtualObjects);
            foreach(string objectClass in m_ontologySystem.objectClassInOntology)
            {
                try
                {
                    var match = m_classMatching.Find(match => match.ClassInOntology.Equals(objectClass));
                    ObjectTag objectTag = (match.ClassInOntology == null) ? (ObjectTag)Enum.Parse(typeof(ObjectTag),objectClass) : match.ClassInVirtualEnvironment;
                    m_detectableObjects.AddRange(objectsInTheHouse.FindAll(o => o.tags.Contains(objectTag)));
                }
                catch (ArgumentException)
                {
                    Log(objectClass + " failed to convert to ObjectTag", LogLevel.Developer);
                }                
            }
            m_detectableObjects = m_detectableObjects.Distinct().ToList();
        }           

        private void Log(string _msg, LogLevel lvl, bool Warning = false)
        {
            if (LogLevel <= lvl && LogLevel != LogLevel.Nothing)
            {
                if (Warning)
                {
                    Debug.LogWarning("[Detection Results]: " + _msg);
                }
                else
                {
                    Debug.Log("[Detection Results]: " + _msg);
                }
            }
        }
        #endregion

    }
}