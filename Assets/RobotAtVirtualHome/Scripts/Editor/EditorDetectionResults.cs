using UnityEngine;
using UnityEditor;

namespace RobotAtVirtualHome.Utils
{
    [CustomEditor(typeof(DetectionResults))]
    public class EditorDetectionResults : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (GUILayout.Button("Calculate Results"))
            {
                ((DetectionResults)target).CalculateResults();
            }

            
        }
    }
}