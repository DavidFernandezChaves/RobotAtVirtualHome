using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ViMantic;

namespace RobotAtVirtualHome.Utils
{
    [RequireComponent(typeof(VirtualObjectSystem), typeof(OntologySystem))]
    public class DetectionResults : MonoBehaviour
    {
        [Serializable]
        public struct ClassMatching
        {
            public string ClassInOntology;
            public ObjectTag ClassInVirtualEnvironment;
        }

        [Header("General")]
        [Tooltip("The log level to use")]
        public LogLevel LogLevel = LogLevel.Normal;
        [SerializeField]
        private float m_maxDistance;
        [SerializeField]
        private int m_minDetections;

        [SerializeField]
        private EnvironmentManager m_enviromentManager;

        [SerializeField]
        private List<ClassMatching> m_classMatching;

        private VirtualObjectSystem m_virtualObjectSystem;
        private OntologySystem m_ontologySystem;
        private List<VirtualObject> m_detectableObjects;

        [SerializeField]
        private Dictionary<SemanticObject, float> detectionsProcessed;

        #region Unity Functions
        private void Awake()
        {
            m_virtualObjectSystem = GetComponent<VirtualObjectSystem>();
            m_ontologySystem = GetComponent<OntologySystem>();
            m_detectableObjects = new List<VirtualObject>();
            detectionsProcessed = new Dictionary<SemanticObject, float>();
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
        private void OnDrawGizmos()
        {
            if (Application.isPlaying && this.enabled && LogLevel == LogLevel.Developer)
            {
                foreach (KeyValuePair<SemanticObject, float> dp in detectionsProcessed)
                {
                    if (dp.Value < m_maxDistance)
                    {
                        Gizmos.color = new Color(0, 1, 0, 0.3f);
                    }
                    else
                    {
                        Gizmos.color = new Color(1,0,0,0.3f);                        
                    }
                    Gizmos.DrawSphere(dp.Key.Position, dp.Value);
                }
            }
        }
#endif

        #endregion

        #region Public Functions
        public void CalculateResults()
        {
            List<VirtualObject> objectsDetected = new List<VirtualObject>();
            int TP = 0, FP = 0, FN = 0;
            float averageDistance = 0;
            float distance = 0;
            ObjectTag objectTag;
            ClassMatching match;
            VirtualObject bestMatch;
            float bestDistance;

            //Calculate TP, FP and FN
            foreach (SemanticObject so in m_virtualObjectSystem.m_objectDetected)
            {
                if (so.NDetections >= m_minDetections)
                {
                    bestMatch = null;
                    bestDistance = m_maxDistance;

                    try
                    {
                        match = m_classMatching.Find(match => match.ClassInOntology.Equals(so.ObjectClass));
                        objectTag = (match.ClassInOntology == null) ? (ObjectTag)Enum.Parse(typeof(ObjectTag), so.ObjectClass) : match.ClassInVirtualEnvironment;

                        foreach (VirtualObject virtualObject in m_detectableObjects)
                        {
                            if (virtualObject.tags.Contains(objectTag))
                            {
                                distance = Vector3.Distance(virtualObject.transform.position, so.Position);
                                if (distance <= bestDistance)
                                {
                                    bestMatch = virtualObject;
                                    bestDistance = distance;
                                }
                            }
                        }

                        detectionsProcessed.Add(so, bestDistance);

                        if (bestMatch != null)
                        {
                            TP++;
                            objectsDetected.Add(bestMatch);
                            averageDistance += bestDistance;
                        }
                        else
                        {
                            FP++;
                        }
                    }
                    catch (ArgumentException)
                    {
                        Log(so.ObjectClass + " failed to convert to ObjectTag", LogLevel.Developer);
                    }
                }
            }
            averageDistance /= objectsDetected.Count;
            FN = m_detectableObjects.Count - objectsDetected.Count;
            //---------------------------------

            Log("TP: " + TP.ToString(), LogLevel.Normal);
            Log("FP: " + FP.ToString(), LogLevel.Normal);
            Log("FN: " + FN.ToString(), LogLevel.Normal);

            float accuracy = (float)TP / (TP + FP);
            float recall = (float)TP / (TP + FN);
            Log("Accuracy: " + accuracy.ToString(), LogLevel.Normal);
            Log("Recall: " + recall.ToString(), LogLevel.Normal);
            Log("F1: " + (2 * accuracy * recall / (accuracy + recall)).ToString("f"), LogLevel.Normal);

            Log("Average Distance: " + averageDistance.ToString(), LogLevel.Normal);
        }
        #endregion

        #region Private Functions
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
            objectsInTheHouse = objectsInTheHouse.Distinct().ToList();
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