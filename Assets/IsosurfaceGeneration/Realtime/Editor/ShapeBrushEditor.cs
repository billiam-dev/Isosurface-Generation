using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.Rendering;
using UnityEngine;

namespace IsosurfaceGeneration.RealtimeEditor
{
    [CustomEditor(typeof(ShapeBrush))]
    class ShapeBrushEditor : Editor
    {
        SerializedProperty m_Shape;
        SerializedProperty m_BlendMode;
        SerializedProperty m_SmoothnessConstant;
        SerializedProperty m_Dimention1;
        SerializedProperty m_Dimention2;

        SphereBoundsHandle m_SphereBoundsHandle;
        ShapeBrush m_Target;

        void OnEnable()
        {
            var o = new PropertyFetcher<ShapeBrush>(serializedObject);

            m_Shape = o.Find(x => x.Shape);
            m_BlendMode = o.Find(x => x.BlendMode);
            m_SmoothnessConstant = o.Find(x => x.Sharpness);
            m_Dimention1 = o.Find(x => x.Dimention1);
            m_Dimention2 = o.Find(x => x.Dimention2);

            m_SphereBoundsHandle = new();

            m_Target = (ShapeBrush)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_Shape, new GUIContent("Shape"));
            EditorGUILayout.PropertyField(m_BlendMode, new GUIContent("Blend Mode"));
            EditorGUILayout.PropertyField(m_SmoothnessConstant, new GUIContent("Sharpness"));

            switch (m_Target.Shape)
            {
                case ShapeFuncion.Sphere:
                    EditorGUILayout.PropertyField(m_Dimention1, new GUIContent("Radius"));
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }

        void OnSceneGUI()
        {
            // Draw editor bounds handle.
            Handles.matrix = m_Target.transform.localToWorldMatrix;
            Handles.color = Color.green;

            EditorGUI.BeginChangeCheck();

            switch (m_Target.Shape)
            {
                case ShapeFuncion.Sphere:
                    DrawSphereHandle();
                    break;
            }
        }

        void DrawSphereHandle()
        {
            m_SphereBoundsHandle.center = Vector3.zero;
            m_SphereBoundsHandle.radius = m_Target.Dimention1;

            m_SphereBoundsHandle.DrawHandle();
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Edited Sphere");
                m_Target.Dimention1 = m_SphereBoundsHandle.radius;
                m_Target.PropertyChanged = true;
            }
        }
    }
}
