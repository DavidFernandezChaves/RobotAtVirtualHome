using UnityEngine;
using UnityEditor;


namespace RobotAtVirtualHome
{
    [CustomEditor(typeof(EnvironmentManager))]

    public class EditorEnviromentManager : Editor
    {
        public override void OnInspectorGUI()
        {

            GUILayout.Label("Preference Settings:");
            if (GUILayout.Button("Save"))
            {
                PlayerPrefs.SetString("generalManagerPath", ((EnvironmentManager)target).path);
                PlayerPrefs.SetInt("generalManagerHouse", ((EnvironmentManager)target).houseSelected);
                PlayerPrefs.Save();
            }
            if (GUILayout.Button("Load"))
            {
                ((EnvironmentManager)target).path = PlayerPrefs.GetString("generalManagerPath");
                ((EnvironmentManager)target).houseSelected = PlayerPrefs.GetInt("generalManagerHouse");
            }

            base.OnInspectorGUI();
        }
    }
}