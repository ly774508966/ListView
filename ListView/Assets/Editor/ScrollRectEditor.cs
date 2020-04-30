using UGUI;
using UnityEditor;
using UnityEngine;

namespace XEngine.Unity.UGUI.Editor
{
    [CustomEditor(typeof(ScrollableRect))]
    public class ScrollRectEditor : UnityEditor.Editor
    {
        void OnEnable()
        {
            prefabSources = serializedObject.FindProperty("prefabSources");
            isLoop = serializedObject.FindProperty("isLoop");
            content = serializedObject.FindProperty("content");
            totalCount = serializedObject.FindProperty("totalCount");
            decelerationRate = serializedObject.FindProperty("decelerationRate");
            slowDownCoefficient = serializedObject.FindProperty("slowDownCoefficient");
            viewRect = serializedObject.FindProperty("viewRect");
            scrollDirection = serializedObject.FindProperty("scrollDirection");
            bounceType = serializedObject.FindProperty("bounceType");
            renderOrder = serializedObject.FindProperty("renderOrder");
            startSpeed = serializedObject.FindProperty("startSpeed");
            bounceSmooth = serializedObject.FindProperty("bounceSmooth");
            bounceEnd = serializedObject.FindProperty("bounceEnd");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(prefabSources, true);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(isLoop);

            EditorGUILayout.PropertyField(content);
            EditorGUILayout.PropertyField(decelerationRate);
            EditorGUILayout.PropertyField(slowDownCoefficient);
            EditorGUILayout.PropertyField(viewRect);

            EditorGUILayout.PropertyField(scrollDirection);
            EditorGUILayout.PropertyField(bounceType);
            EditorGUI.indentLevel++;
            switch (bounceType.intValue)
            {
                case 0:
                    break;
                case 1:
                    EditorGUILayout.PropertyField(startSpeed);

                    break;
                case 2:
                    EditorGUILayout.PropertyField(bounceEnd);
                    EditorGUILayout.PropertyField(bounceSmooth);

                    break;
                case 3:
                    EditorGUILayout.PropertyField(startSpeed);
                    EditorGUILayout.PropertyField(bounceEnd);
                    EditorGUILayout.PropertyField(bounceSmooth, new GUIContent("Bounce End Smooth"));

                    break;
                default:
                    Debug.LogError("不支持的类型");

                    break;
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.PropertyField(renderOrder);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(totalCount);

            serializedObject.GetIterator().NextVisible(true);
            serializedObject.ApplyModifiedProperties();
        }

        private SerializedProperty prefabSources;
        private SerializedProperty isLoop;
        private SerializedProperty content;
        private SerializedProperty totalCount;
        private SerializedProperty decelerationRate;
        private SerializedProperty slowDownCoefficient;
        private SerializedProperty viewRect;
        private SerializedProperty scrollDirection;
        private SerializedProperty bounceType;
        private SerializedProperty renderOrder;
        private SerializedProperty startSpeed;
        private SerializedProperty bounceSmooth;
        private SerializedProperty bounceEnd;
    }

}