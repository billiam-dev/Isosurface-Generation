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
        SerializedProperty m_InvertTerrain;
        SerializedProperty m_Material;

        void OnEnable()
        {
            var o = new PropertyFetcher<ProceduralTerrain>(serializedObject);

            m_Dimentions = o.Find(x => x.Dimentions);
            m_IsoLevel = o.Find(x => x.IsoLevel);
            m_InvertTerrain = o.Find(x => x.InvertTerrain);
            m_Material = o.Find(x => x.Material);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_Dimentions, new GUIContent("Dimentions"));
            EditorGUILayout.PropertyField(m_IsoLevel, new GUIContent("Iso Level"));
            EditorGUILayout.PropertyField(m_InvertTerrain, new GUIContent("Invert Terrain"));
            EditorGUILayout.PropertyField(m_Material, new GUIContent("Material"));

            serializedObject.ApplyModifiedProperties();
        }
    }
}
