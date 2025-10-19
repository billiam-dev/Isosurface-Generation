using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace IsosurfaceGeneration.Input
{
    // See: https://github.com/CristianQiu/Unity-Editor-PolymorphicReorderableList/blob/master/Assets/Code/Editor/BaseCharacterEditor.cs

    [CustomEditor(typeof(RealtimeShapesInput), true)]
    public class RealtimeShapesInputEditor : Editor
    {
        SerializedProperty m_Brushes;
        ReorderableList m_BrushesList;
        RealtimeShapesInput m_Target;

        static readonly Color k_ProSkinSelectionBgColor = new(0.1725f, 0.3647f, 0.5294f, 1.0f);
        static readonly Color k_PersonalSkinSelectionBgColor = new(0.2274f, 0.447f, 0.6901f, 1.0f);

        const float k_HeaderHeight = 20.0f;
        const float k_HeaderXOffset = 15.0f;
        const float k_MarginReorderIcon = 20.0f;

        float DefaultLineSpacing => EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        void OnEnable()
        {
            m_Target = (RealtimeShapesInput)target;

            m_Brushes = serializedObject.FindProperty("m_Brushes");

            m_BrushesList = new ReorderableList(serializedObject, m_Brushes, true, true, true, true);
            m_BrushesList.drawHeaderCallback += OnDrawReorderListHeader;
            m_BrushesList.drawElementCallback += OnDrawReorderListElement;
            m_BrushesList.drawElementBackgroundCallback += OnDrawReorderListBg;
            m_BrushesList.elementHeightCallback += OnReorderListElementHeight;
            m_BrushesList.onAddDropdownCallback += OnReorderListAddDropdown;
            m_BrushesList.onDeleteArrayElementCallback += OnDeleteElement;
            //m_BrushesList.onReorderCallback += ;
        }

        void OnDisable()
        {
            m_BrushesList.drawHeaderCallback -= OnDrawReorderListHeader;
            m_BrushesList.drawElementCallback -= OnDrawReorderListElement;
            m_BrushesList.drawElementBackgroundCallback -= OnDrawReorderListBg;
            m_BrushesList.elementHeightCallback -= OnReorderListElementHeight;
            m_BrushesList.onAddDropdownCallback -= OnReorderListAddDropdown;
            m_BrushesList.onDeleteArrayElementCallback -= OnDeleteElement;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            m_BrushesList.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
        }

        void OnDrawReorderListHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Brushes");
        }

        void OnDrawReorderListElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (m_BrushesList.serializedProperty.arraySize == 0)
                return;

            SerializedProperty brushProperty = m_BrushesList.serializedProperty.GetArrayElementAtIndex(index);

            Rect foldoutHeaderRect = rect;
            foldoutHeaderRect.height = k_HeaderHeight;
            foldoutHeaderRect.x += k_HeaderXOffset;
            foldoutHeaderRect.width -= k_HeaderXOffset;

            brushProperty.isExpanded = EditorGUI.BeginFoldoutHeaderGroup(foldoutHeaderRect, brushProperty.isExpanded, brushProperty.objectReferenceValue.name);
            if (brushProperty.isExpanded)
            {
                rect.y += DefaultLineSpacing;
                rect.height = EditorGUIUtility.singleLineHeight;

                GUI.enabled = false;
                EditorGUI.PropertyField(rect, brushProperty, true);
                rect.y += DefaultLineSpacing;
                GUI.enabled = true;

                ShapeBrushEditor brushEditor = (ShapeBrushEditor)CreateEditor(m_Brushes.GetArrayElementAtIndex(index).objectReferenceValue);
                brushEditor.DrawListInspector(rect, DefaultLineSpacing);
            }

            EditorGUI.EndFoldoutHeaderGroup();
        }

        void OnDrawReorderListBg(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (!isFocused || !isActive)
                return;

            float height = OnReorderListElementHeight(index);

            SerializedProperty brushProperty = m_BrushesList.serializedProperty.GetArrayElementAtIndex(index);

            // Remove a bit of the line that goes beyond the header label.
            if (!brushProperty.isExpanded)
                height -= EditorGUIUtility.standardVerticalSpacing;

            Rect copyRect = rect;
            copyRect.width = k_MarginReorderIcon;
            copyRect.height = height;

            // Draw two rects indepently to avoid overlapping the header label.
            Color color = EditorGUIUtility.isProSkin ? k_ProSkinSelectionBgColor : k_PersonalSkinSelectionBgColor;
            EditorGUI.DrawRect(copyRect, color);

            float offset = 2.0f;
            rect.x += k_MarginReorderIcon;
            rect.width -= k_MarginReorderIcon + offset;

            rect.height = height - k_HeaderHeight + offset;
            rect.y += k_HeaderHeight - offset;

            EditorGUI.DrawRect(rect, color);
        }

        float OnReorderListElementHeight(int index) 
        {
            if (m_BrushesList.serializedProperty.arraySize == 0)
                return 0.0f;

            SerializedProperty brushProperty = m_BrushesList.serializedProperty.GetArrayElementAtIndex(index);
            if (!brushProperty.isExpanded)
                return DefaultLineSpacing;

            return DefaultLineSpacing * 6.0f;
        }

        void OnReorderListAddDropdown(Rect buttonRect, ReorderableList list)
        {
            GenericMenu menu = new();
            menu.AddItem(new GUIContent("Sphere"), false, OnAddItemFromDropdown, ShapeFunction.Sphere);
            menu.AddItem(new GUIContent("Semi Sphere"), false, OnAddItemFromDropdown, ShapeFunction.SemiSphere);
            menu.AddItem(new GUIContent("Capsule"), false, OnAddItemFromDropdown, ShapeFunction.Capsule);
            menu.AddItem(new GUIContent("Torus"), false, OnAddItemFromDropdown, ShapeFunction.Torus);

            menu.ShowAsContext();
        }

        void OnAddItemFromDropdown(object userData)
        {
            m_Target.AddShape((ShapeFunction)userData);
        }

        void OnDeleteElement(ReorderableList list, int index)
        {
            // Does not work for some reason?
            Debug.Log($"Delete element {list}, {index}");
            m_Target.RemoveShape(index);
        }
    }
}