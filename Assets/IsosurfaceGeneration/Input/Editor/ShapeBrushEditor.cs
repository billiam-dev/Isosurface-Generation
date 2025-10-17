using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.Rendering;
using UnityEngine;

namespace IsosurfaceGeneration.Input
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ShapeBrush))]
    class ShapeBrushEditor : Editor
    {
        SerializedProperty m_Shape;
        SerializedProperty m_BlendMode;
        SerializedProperty m_SmoothnessConstant;
        SerializedProperty m_Dimention1;
        SerializedProperty m_Dimention2;

        SphereBoundsHandle m_SphereBoundsHandle;
        CapsuleBoundsHandle m_CapsuleBoundsHandle;
        TorusBoundsHandle m_TorusBoundsHandle;

        ShapeBrush m_Target;

        void OnEnable()
        {
            var o = new PropertyFetcher<ShapeBrush>(serializedObject);

            m_Shape = o.Find(x => x.Shape);
            m_BlendMode = o.Find(x => x.BlendMode);
            m_SmoothnessConstant = o.Find(x => x.Sharpness);
            m_Dimention1 = o.Find(x => x.Dimention1);
            m_Dimention2 = o.Find(x => x.Dimention2);

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

                case ShapeFuncion.SemiSphere:
                    EditorGUILayout.PropertyField(m_Dimention1, new GUIContent("Radius"));
                    m_Dimention2.floatValue = EditorGUILayout.Slider(new GUIContent("Slice"), m_Dimention2.floatValue / m_Dimention1.floatValue, -0.99f, 0.99f) * m_Dimention1.floatValue;
                    break;

                case ShapeFuncion.Capsule:
                    EditorGUILayout.PropertyField(m_Dimention1, new GUIContent("Height"));
                    EditorGUILayout.PropertyField(m_Dimention2, new GUIContent("Radius"));
                    break;

                case ShapeFuncion.Torus:
                    EditorGUILayout.PropertyField(m_Dimention1, new GUIContent("Outer Radius"));
                    EditorGUILayout.PropertyField(m_Dimention2, new GUIContent("Inner Radius"));
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

                case ShapeFuncion.SemiSphere:
                    DrawSphereHandle();
                    break;

                case ShapeFuncion.Capsule:
                    DrawCapsuleHandle();
                    break;

                case ShapeFuncion.Torus:
                    DrawTorusHandle();
                    break;
            }
        }

        void DrawSphereHandle()
        {
            if (m_SphereBoundsHandle == null)
                m_SphereBoundsHandle = new();

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

        void DrawCapsuleHandle()
        {
            if (m_CapsuleBoundsHandle == null)
                m_CapsuleBoundsHandle = new();

            m_CapsuleBoundsHandle.center = Vector3.zero;
            m_CapsuleBoundsHandle.height = (m_Target.Dimention1 + m_Target.Dimention2) * 2;
            m_CapsuleBoundsHandle.radius = m_Target.Dimention2;

            m_CapsuleBoundsHandle.DrawHandle();
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Edited Capsule");

                m_Target.Dimention1 = (m_CapsuleBoundsHandle.height / 2) - m_CapsuleBoundsHandle.radius;
                m_Target.Dimention2 = m_CapsuleBoundsHandle.radius;
                m_Target.PropertyChanged = true;
            }
        }

        void DrawTorusHandle()
        {
            if (m_TorusBoundsHandle == null)
                m_TorusBoundsHandle = new();

            m_TorusBoundsHandle.center = Vector3.zero;
            m_TorusBoundsHandle.radius = m_Target.Dimention1;

            m_TorusBoundsHandle.DrawHandle();
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Edited Torus");

                m_Target.Dimention1 = m_TorusBoundsHandle.radius;
                m_Target.PropertyChanged = true;
            }
        }
    }
}
