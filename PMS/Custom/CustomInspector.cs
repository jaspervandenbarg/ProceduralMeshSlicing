using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom ispector for the slicer script
/// This custom inspector ads a button to slice while in scene view
/// </summary>

[CustomEditor(typeof(MeshSlicer))]
public class CustomInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        MeshSlicer slicer = (MeshSlicer)target;
        if (GUILayout.Button("Slice") && Application.isPlaying)
        {
            slicer.Slice();
        }
    }
}
