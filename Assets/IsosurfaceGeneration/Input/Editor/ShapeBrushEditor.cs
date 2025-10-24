using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace IsosurfaceGeneration.Input
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ShapeBrush), true)]
    public class ShapeBrushEditor : Editor
    {
        SerializedProperty m_ShapeType;
        SerializedProperty m_BlendMode;
        SerializedProperty m_Sharpness;
        SerializedProperty m_Dimention1;
        SerializedProperty m_Dimention2;

        SphereBoundsHandle m_SphereBoundsHandle;
        CapsuleBoundsHandle m_CapsuleBoundsHandle;
        TorusBoundsHandle m_TorusBoundsHandle;

        ShapeBrush m_Target;

        void OnEnable()
        {
            m_ShapeType = serializedObject.FindProperty("m_ShapeType");
            m_BlendMode = serializedObject.FindProperty("m_BlendMode");
            m_Sharpness = serializedObject.FindProperty("m_Sharpness");
            m_Dimention1 = serializedObject.FindProperty("m_Dimention1");
            m_Dimention2 = serializedObject.FindProperty("m_Dimention2");

            m_Target = (ShapeBrush)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_ShapeType, new GUIContent("Shape"));
            EditorGUILayout.PropertyField(m_BlendMode, new GUIContent("Blend Mode"));
            EditorGUILayout.PropertyField(m_Sharpness, new GUIContent("Sharpness"));

            switch ((ShapeFunction)m_ShapeType.enumValueIndex)
            {
                case ShapeFunction.Sphere:
                    EditorGUILayout.PropertyField(m_Dimention1, new GUIContent("Radius"));
                    break;

                case ShapeFunction.SemiSphere:
                    EditorGUILayout.PropertyField(m_Dimention1, new GUIContent("Radius"));
                    m_Dimention2.floatValue = EditorGUILayout.Slider(new GUIContent("Slice"), m_Dimention2.floatValue / m_Dimention1.floatValue, -0.99f, 0.99f) * m_Dimention1.floatValue;
                    break;

                case ShapeFunction.Capsule:
                    EditorGUILayout.PropertyField(m_Dimention1, new GUIContent("Height"));
                    EditorGUILayout.PropertyField(m_Dimention2, new GUIContent("Radius"));
                    break;

                case ShapeFunction.Torus:
                    EditorGUILayout.PropertyField(m_Dimention1, new GUIContent("Outer Radius"));
                    EditorGUILayout.PropertyField(m_Dimention2, new GUIContent("Inner Radius"));
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }

        public void DrawListInspector(Rect rect, float lineSpacing)
        {
            serializedObject.Update();

            EditorGUI.PropertyField(rect, m_ShapeType, new GUIContent("Shape"));
            rect.y += lineSpacing;

            EditorGUI.PropertyField(rect, m_BlendMode, new GUIContent("Blend Mode"));
            rect.y += lineSpacing;

            EditorGUI.PropertyField(rect, m_Sharpness, new GUIContent("Sharpness"));
            rect.y += lineSpacing;

            switch ((ShapeFunction)m_ShapeType.enumValueIndex)
            {
                case ShapeFunction.Sphere:
                    EditorGUI.PropertyField(rect, m_Dimention1, new GUIContent("Radius"));
                    rect.y += lineSpacing;
                    break;

                case ShapeFunction.SemiSphere:
                    EditorGUI.PropertyField(rect, m_Dimention1, new GUIContent("Radius"));
                    rect.y += lineSpacing;
                    m_Dimention2.floatValue = EditorGUI.Slider(rect, new GUIContent("Slice"), m_Dimention2.floatValue / m_Dimention1.floatValue, -0.99f, 0.99f) * m_Dimention1.floatValue;
                    rect.y += lineSpacing;

                    break;

                case ShapeFunction.Capsule:
                    EditorGUI.PropertyField(rect, m_Dimention1, new GUIContent("Height"));
                    rect.y += lineSpacing;

                    EditorGUI.PropertyField(rect, m_Dimention2, new GUIContent("Radius"));
                    rect.y += lineSpacing;
                    break;

                case ShapeFunction.Torus:
                    EditorGUI.PropertyField(rect, m_Dimention1, new GUIContent("Outer Radius"));
                    rect.y += lineSpacing;

                    EditorGUI.PropertyField(rect, m_Dimention2, new GUIContent("Inner Radius"));
                    rect.y += lineSpacing;
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }

        void OnSceneGUI()
        {
            Handles.matrix = m_Target.transform.localToWorldMatrix;
            Handles.color = Color.green;

            EditorGUI.BeginChangeCheck();

            switch ((ShapeFunction)m_ShapeType.enumValueIndex)
            {
                case ShapeFunction.Sphere:
                    DrawSphereHandle();
                    break;

                case ShapeFunction.SemiSphere:
                    DrawSphereHandle();
                    break;

                case ShapeFunction.Capsule:
                    DrawCapsuleHandle();
                    break;

                case ShapeFunction.Torus:
                    DrawTorusHandle();
                    break;
            }
        }

        void DrawSphereHandle()
        {
            m_SphereBoundsHandle ??= new();

            m_SphereBoundsHandle.center = Vector3.zero;
            m_SphereBoundsHandle.radius = m_Dimention1.floatValue;
            m_SphereBoundsHandle.DrawHandle();

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Edited Sphere");

                m_Dimention1.SetUnderlyingValue(m_SphereBoundsHandle.radius);
                m_Target.IsDirty = true;
            }
        }

        void DrawCapsuleHandle()
        {
            m_CapsuleBoundsHandle ??= new();

            m_CapsuleBoundsHandle.center = Vector3.zero;
            m_CapsuleBoundsHandle.height = (m_Dimention1.floatValue + m_Dimention2.floatValue) * 2;
            m_CapsuleBoundsHandle.radius = m_Dimention2.floatValue;
            m_CapsuleBoundsHandle.DrawHandle();

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Edited Capsule");

                m_Dimention1.SetUnderlyingValue((m_CapsuleBoundsHandle.height / 2) - m_CapsuleBoundsHandle.radius);
                m_Dimention2.SetUnderlyingValue(m_CapsuleBoundsHandle.radius);
                m_Target.IsDirty = true;
            }
        }

        void DrawTorusHandle()
        {
            m_TorusBoundsHandle ??= new();

            m_TorusBoundsHandle.center = Vector3.zero;
            m_TorusBoundsHandle.radius = m_Dimention1.floatValue;
            m_TorusBoundsHandle.DrawHandle();

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Edited Torus");

                m_Dimention1.SetUnderlyingValue(m_TorusBoundsHandle.radius);
                m_Target.IsDirty = true;
            }
        }
    }
}
