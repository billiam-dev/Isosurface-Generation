using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace IsosurfaceGeneration.Input
{
    [CustomEditor(typeof(ShapesInput), true)]
    public class ShapesInputEditor : Editor
    {
        static readonly Color k_ProSkinSelectionBgColor = new(0.1725f, 0.3647f, 0.5294f, 1.0f);
        static readonly Color k_PersonalSkinSelectionBgColor = new(0.2274f, 0.447f, 0.6901f, 1.0f);

        const float k_HeaderHeight = 20.0f;
        const float k_HeaderXOffset = 15.0f;
        const float k_MarginReorderIcon = 20.0f;
        const float k_EnableIconSize = 20.0f;

        float DefaultLineSpacing => EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        SerializedProperty m_ShapeBrushes;
        ReorderableList m_ReorderableList;

        ShapesInput m_Target;

        void OnEnable()
        {
            m_Target = (ShapesInput)target;

            m_ShapeBrushes = serializedObject.FindProperty("m_ShapeBrushes");
            m_ReorderableList = new ReorderableList(serializedObject, m_ShapeBrushes, true, true, true, true);

            m_ReorderableList.drawHeaderCallback += OnDrawReorderListHeader;
            m_ReorderableList.drawElementCallback += OnDrawReorderListElement;
            m_ReorderableList.drawElementBackgroundCallback += OnDrawReorderListBg;
            m_ReorderableList.elementHeightCallback += OnReorderListElementHeight;
            m_ReorderableList.onAddDropdownCallback += OnReorderListAddDropdown;
            m_ReorderableList.onReorderCallbackWithDetails += OnReorderList;
            m_ReorderableList.onDeleteArrayElementCallback += OnDeleteElement;
        }

        void OnDisable()
        {
            m_ReorderableList.drawHeaderCallback -= OnDrawReorderListHeader;
            m_ReorderableList.drawElementCallback -= OnDrawReorderListElement;
            m_ReorderableList.drawElementBackgroundCallback -= OnDrawReorderListBg;
            m_ReorderableList.elementHeightCallback -= OnReorderListElementHeight;
            m_ReorderableList.onAddDropdownCallback -= OnReorderListAddDropdown;
            m_ReorderableList.onReorderCallbackWithDetails -= OnReorderList;
            m_ReorderableList.onDeleteArrayElementCallback -= OnDeleteElement;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            m_ReorderableList.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
        }

        void OnDrawReorderListHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Brushes");
        }

        void OnDrawReorderListElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (m_ReorderableList.serializedProperty.arraySize == 0)
                return;

            SerializedProperty brushProperty = m_ReorderableList.serializedProperty.GetArrayElementAtIndex(index);

            Rect foldoutHeaderRect = rect;
            foldoutHeaderRect.height = k_HeaderHeight;
            foldoutHeaderRect.x += k_HeaderXOffset;
            foldoutHeaderRect.width -= k_HeaderXOffset + k_EnableIconSize;

            Rect toggleRect = foldoutHeaderRect;
            toggleRect.x = toggleRect.width + 60.0f;
            toggleRect.width = EditorGUIUtility.singleLineHeight;

            GameObject brushObject = ((Component)brushProperty.objectReferenceValue).gameObject;
            brushObject.SetActive(EditorGUI.Toggle(toggleRect, brushObject.activeSelf));

            brushProperty.isExpanded = EditorGUI.BeginFoldoutHeaderGroup(foldoutHeaderRect, brushProperty.isExpanded, brushProperty.objectReferenceValue.name);
            if (brushProperty.isExpanded)
            {
                rect.height = EditorGUIUtility.singleLineHeight;
                rect.y += DefaultLineSpacing;

                GUI.enabled = false;
                EditorGUI.PropertyField(rect, brushProperty, true);
                rect.y += DefaultLineSpacing;
                GUI.enabled = true;

                ShapeBrushEditor brushEditor = (ShapeBrushEditor)CreateEditor(m_ShapeBrushes.GetArrayElementAtIndex(index).objectReferenceValue);
                brushEditor.DrawListInspector(rect, DefaultLineSpacing);
            }

            EditorGUI.EndFoldoutHeaderGroup();
        }

        void OnDrawReorderListBg(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (!isFocused || !isActive)
                return;

            float height = OnReorderListElementHeight(index);

            SerializedProperty brushProperty = m_ReorderableList.serializedProperty.GetArrayElementAtIndex(index);

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
            if (m_ReorderableList.serializedProperty.arraySize == 0)
                return 0.0f;

            SerializedProperty brushProperty = m_ReorderableList.serializedProperty.GetArrayElementAtIndex(index);
            if (!brushProperty.isExpanded)
                return DefaultLineSpacing;

            return DefaultLineSpacing * 7;
        }

        void OnReorderListAddDropdown(Rect buttonRect, ReorderableList list)
        {
            GenericMenu menu = new();
            menu.AddItem(new GUIContent("Sphere"), false, OnAddItemFromDropdown, ShapeFunction.Sphere);

            menu.ShowAsContext();
        }

        void OnAddItemFromDropdown(object userData)
        {
            m_Target.AddShapeBrush((ShapeFunction)userData);
        }

        void OnReorderList(ReorderableList list, int oldIndex, int newIndex)
        {
            //Debug.Log($"{oldIndex} -> {newIndex}");
            m_Target.ReorderBrushes(oldIndex, newIndex);
        }

        void OnDeleteElement(ReorderableList list, int index)
        {
            // This just never gets called for some reason...?
            Debug.Log($"Delete element {index}");
            m_Target.DeleteShapeBrush(index);
        }
    }
}
