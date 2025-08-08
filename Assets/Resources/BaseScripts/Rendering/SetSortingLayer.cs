using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using System.Reflection;
#endif

[RequireComponent(typeof(Renderer))]
public class SetSortingLayer : MonoBehaviour
{

    [HideInInspector]
    public int sortingLayerIndex;
    public int sortingOrder;

#if UNITY_EDITOR
    [CustomEditor(typeof(SetSortingLayer))]
    public class SetSortingLayerEditor : Editor
    {
        string[] _choices;

        public override void OnInspectorGUI()
        {
            var sorter = target as SetSortingLayer;

            DrawDefaultInspector();

            _choices = GetSortingLayerNames();

            EditorGUILayout.LabelField("");
            EditorGUILayout.LabelField("Sorting Layer", EditorStyles.boldLabel);
            sorter.sortingLayerIndex = EditorGUILayout.Popup(sorter.sortingLayerIndex, _choices);

            if (_choices.Length > 0)
            {
                sorter.GetComponent<Renderer>().sortingLayerName = _choices[sorter.sortingLayerIndex];
                sorter.GetComponent<Renderer>().sortingOrder = sorter.sortingOrder;
            }

            EditorUtility.SetDirty(target);
        }

        public string[] GetSortingLayerNames()
        {
            var internalEditorUtilityType = typeof(InternalEditorUtility);
            PropertyInfo sortingLayersProperty = internalEditorUtilityType.GetProperty("sortingLayerNames", BindingFlags.Static | BindingFlags.NonPublic);
            return (string[])sortingLayersProperty.GetValue(null, new object[0]);
        }
    }
#endif
}