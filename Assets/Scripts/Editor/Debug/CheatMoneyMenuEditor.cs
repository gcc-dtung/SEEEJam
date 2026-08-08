using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CheatMoneyMenu))]
public class CheatMoneyMenuEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        CheatMoneyMenu cheatMenu = (CheatMoneyMenu)target;

        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            if (GUILayout.Button("Increase Money"))
                cheatMenu.IncreaseMoney();

            if (GUILayout.Button("Decrease Money"))
                cheatMenu.DecreaseMoney();
        }

        if (!Application.isPlaying)
            EditorGUILayout.HelpBox("Enter Play Mode to use the money cheat buttons.", MessageType.Info);
    }
}