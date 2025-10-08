using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

namespace TerrainGeneration
{
    [CustomEditor(typeof(ProceduralTerrain))]
    public class ProceduralTerrainEditor : Editor
    {
        SerializedProperty m_Dimentions;
        SerializedProperty m_IsoLevel;
        SerializedProperty m_InvertFaces;
        SerializedProperty m_Material;

        void OnEnable()
        {
            var o = new PropertyFetcher<ProceduralTerrain>(serializedObject);

            m_Dimentions = o.Find(x => x.Dimentions);
            m_IsoLevel = o.Find(x => x.IsoLevel);
            m_InvertFaces = o.Find(x => x.InvertFaces);
            m_Material = o.Find(x => x.Material);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_Dimentions, new GUIContent("Dimentions"));
            EditorGUILayout.PropertyField(m_IsoLevel, new GUIContent("Iso Level"));
            EditorGUILayout.PropertyField(m_InvertFaces, new GUIContent("Invert Faces"));
            EditorGUILayout.PropertyField(m_Material, new GUIContent("Material"));

            serializedObject.ApplyModifiedProperties();
        }
    }
}
