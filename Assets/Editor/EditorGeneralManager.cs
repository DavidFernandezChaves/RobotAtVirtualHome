using UnityEngine;
using UnityEditor;
using RobotAtVirtualHome;
[CustomEditor(typeof(GeneralManager))]

public class EditorGeneralManager : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        GUILayout.Label("Preference Settings:");
        if (GUILayout.Button("Save"))
        {
            PlayerPrefs.SetString("generalManagerPath", ((GeneralManager)target).path);
            PlayerPrefs.SetInt("generalManagerHouse", ((GeneralManager)target).houseSelected);
            PlayerPrefs.Save();
        }
        if (GUILayout.Button("Load"))
        {
            ((GeneralManager)target).path = PlayerPrefs.GetString("generalManagerPath");
            ((GeneralManager)target).houseSelected = PlayerPrefs.GetInt("generalManagerHouse");
        }

    }
}