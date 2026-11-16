using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        GUI.enabled = false; // Disables editing
        EditorGUI.PropertyField(position, property, label, true);
        GUI.enabled = true;  // Re-enables editing for subsequent fields
    }
}
