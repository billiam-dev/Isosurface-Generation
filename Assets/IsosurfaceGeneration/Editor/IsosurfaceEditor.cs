using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

namespace IsosurfaceGeneration
{
    [CustomEditor(typeof(Isosurface))]
    public class IsosurfaceEditor : Editor
    {
        SerializedProperty m_MeshingMethod;
        SerializedProperty m_Dimentions;
        SerializedProperty m_ChunkSize;
        SerializedProperty m_IsoLevel;
        SerializedProperty m_InvertSurface;
        SerializedProperty m_Material;

        void OnEnable()
        {
            var o = new PropertyFetcher<Isosurface>(serializedObject);

            m_MeshingMethod = o.Find(x => x.MeshingMethod);
            m_Dimentions = o.Find(x => x.Dimentions);
            m_ChunkSize = o.Find(x => x.ChunkSize);
            m_IsoLevel = o.Find(x => x.IsoLevel);
            m_InvertSurface = o.Find(x => x.InvertSurface);
            m_Material = o.Find(x => x.Material);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_MeshingMethod, new GUIContent("Meshing Method"));
            EditorGUILayout.PropertyField(m_Dimentions, new GUIContent("Dimentions"));
            EditorGUILayout.PropertyField(m_ChunkSize, new GUIContent("Chunk Size"));
            EditorGUILayout.PropertyField(m_IsoLevel, new GUIContent("Iso Level"));
            EditorGUILayout.PropertyField(m_InvertSurface, new GUIContent("Invert Surface"));
            EditorGUILayout.PropertyField(m_Material, new GUIContent("Material"));

            serializedObject.ApplyModifiedProperties();
        }
    }
}
