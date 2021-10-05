using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace RobotAtVirtualHome
{

    [CustomEditor(typeof(Initializer))]

    public class EditorInitializer : Editor
    {

        public override void OnInspectorGUI()
        {

            GUILayout.Label("Preference Settings:");
            if (GUILayout.Button("Save"))
            {
                List<Initializer.Agent> agentToInstantiate = ((Initializer)target).agentToInstantiate;
                if (agentToInstantiate.Count > 0)
                {
                    string names = "";
                    string ips = "";
                    string paths = "";
                    foreach (Initializer.Agent a in agentToInstantiate)
                    {
                        names += a.name + ";";
                        ips += a.ip + ";";
                        paths += AssetDatabase.GetAssetPath(a.prefab) + ";";
                    }

                    PlayerPrefs.SetString("robotNames", names);
                    PlayerPrefs.SetString("ips", ips);
                    PlayerPrefs.SetString("paths", paths);
                    PlayerPrefs.Save();
                }
            }
            if (GUILayout.Button("Load"))
            {
                List<Initializer.Agent> agentToInstantiate = new List<Initializer.Agent>();
                string stnames = PlayerPrefs.GetString("robotNames", "");
                string stips = PlayerPrefs.GetString("ips", "");
                string stpaths = PlayerPrefs.GetString("paths", "");

                string[] names = stnames.Split(';');
                string[] ips = stips.Split(';');
                string[] paths = stpaths.Split(';');

                for (int i = 0; i < names.Length - 1; i++)
                {
                    agentToInstantiate.Add(new Initializer.Agent(names[i], ips[i], paths[i]));
                }
                ((Initializer)target).agentToInstantiate = agentToInstantiate;
            }

            base.OnInspectorGUI();
        }
    }
}