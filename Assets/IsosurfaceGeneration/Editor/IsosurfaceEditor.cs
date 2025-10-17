using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

namespace IsosurfaceGeneration
{
    [CustomEditor(typeof(Isosurface))]
    public class IsosurfaceEditor : Editor
    {
        SerializedProperty m_MeshingMethod;
        SerializedProperty m_DensityMethod;
        SerializedProperty m_Dimentions;
        SerializedProperty m_ChunkSize;
        SerializedProperty m_IsoLevel;
        SerializedProperty m_InvertSurface;
        SerializedProperty m_Material;
        SerializedProperty m_SendLogMessages;

        void OnEnable()
        {
            var o = new PropertyFetcher<Isosurface>(serializedObject);

            m_MeshingMethod = o.Find(x => x.MeshingMethod);
            m_DensityMethod = o.Find(x => x.DensityMethod);
            m_Dimentions = o.Find(x => x.Dimentions);
            m_ChunkSize = o.Find(x => x.ChunkSize);
            m_IsoLevel = o.Find(x => x.IsoLevel);
            m_InvertSurface = o.Find(x => x.InvertSurface);
            m_Material = o.Find(x => x.Material);
            m_SendLogMessages = o.Find(x => x.SendLogMessages);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_MeshingMethod, new GUIContent("Meshing Method"));
            EditorGUILayout.PropertyField(m_DensityMethod, new GUIContent("Density Method"));
            EditorGUILayout.PropertyField(m_Dimentions, new GUIContent("Dimentions"));
            EditorGUILayout.PropertyField(m_ChunkSize, new GUIContent("Chunk Size"));
            EditorGUILayout.PropertyField(m_IsoLevel, new GUIContent("Iso Level"));
            EditorGUILayout.PropertyField(m_InvertSurface, new GUIContent("Invert Surface"));
            EditorGUILayout.PropertyField(m_Material, new GUIContent("Material"));
            EditorGUILayout.PropertyField(m_SendLogMessages, new GUIContent("Log Profiling"));

            serializedObject.ApplyModifiedProperties();
        }
    }
}
