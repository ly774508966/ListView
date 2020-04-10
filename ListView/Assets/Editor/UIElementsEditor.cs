using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using UnityEditorInternal;

namespace UGUI.Editor
{
    /// <summary>
    /// 对数据<see cref="UIElements"/>的编辑器绘制
    /// </summary>
    [CustomEditor(typeof(UIElements))]
    public class UIElementsEditor : UnityEditor.Editor
    {
        #region Internal Methods
        private void OnEnable()
        {
            var type = target.GetType();
            var flag = BindingFlags.Instance |
                       BindingFlags.NonPublic |
                       BindingFlags.GetField;

            keys = (List<string>)type.InvokeMember("keys", flag, null,
                target, null);
            values = (List<Object>)type.InvokeMember("values", flag, null,
                target, null);

            if (keys == null || values == null)
            {
                keys = new List<string>();
                values = new List<Object>();
            }

            list = new ReorderableList(keys, typeof(string));
            list.drawHeaderCallback = DrawHeaderCallback;
            list.drawElementCallback = DrawElementCallback;
            list.onAddCallback = OnAddCallback;
            list.onRemoveCallback = OnRemoveCallback;
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.Space();
            serializedObject.Update();
            list.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHeaderCallback(Rect rect)
        {
            GUI.Label(rect, "Targets");

            if (!rect.Contains(Event.current.mousePosition))
            {
                return;
            }

            if (Event.current.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                Event.current.Use();
            }
            else if (Event.current.type == EventType.DragPerform)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                Event.current.Use();
                if (DragAndDrop.objectReferences.Length > 0)
                {
                    var obj = DragAndDrop.objectReferences[0];
                    Undo.RegisterCompleteObjectUndo(target, "addDragItem");
                    keys.Add(obj.name);
                    values.Add(obj);
                    EditorUtility.SetDirty(target);
                }
            }
        }

        private void OnRemoveCallback(ReorderableList reorderAbleList)
        {
            Undo.RegisterCompleteObjectUndo(target, "deleteItem");
            keys.RemoveAt(reorderAbleList.index);
            values.RemoveAt(reorderAbleList.index);
            EditorUtility.SetDirty(target);
            reorderAbleList.index = keys.Count - 1;
        }

        private void OnAddCallback(ReorderableList reorderAbleList)
        {
            Undo.RegisterCompleteObjectUndo(target, "addItem");
            keys.Add("Unique");
            values.Add(null);
            EditorUtility.SetDirty(target);
        }

        private void DrawElementCallback(Rect rect, int index, bool isActive, bool isFocused)
        {
            Rect labelRect = rect;
            labelRect.xMax = EditorGUIUtility.labelWidth - 10;

            EditorGUI.BeginChangeCheck();
            string newName = EditorGUI.TextField(labelRect, keys[index]);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RegisterCompleteObjectUndo(target, "modifyItemKey");
                keys[index] = newName;
                EditorUtility.SetDirty(target);
            }

            Rect valueRect = rect;
            valueRect.xMin = labelRect.xMax;
            EditorGUI.BeginChangeCheck();
            var newValue = EditorGUI.ObjectField(valueRect, values[index], typeof(Object), true);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RegisterCompleteObjectUndo(target, "modifyItemValue");
                values[index] = newValue;
                EditorUtility.SetDirty(target);
            }
        }
        #endregion

        #region Internal Fields
        private ReorderableList list;
        private List<string> keys;
        private List<Object> values;
        #endregion
    }
}