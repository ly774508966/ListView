using UGUI.ListView;
using UnityEditor;

[CustomEditor(typeof(ScrollableRect))]
public class ScrollableRectEditor : Editor
{
    void OnEnable()
    {
        prefabSources = serializedObject.FindProperty("prefabSources");
        localPool = serializedObject.FindProperty("localPool");
        isLoop = serializedObject.FindProperty("isLoop");
        content = serializedObject.FindProperty("content");
        totalCount = serializedObject.FindProperty("totalCount");
        decelerationRate = serializedObject.FindProperty("decelerationRate");
        slowDownCoefficient = serializedObject.FindProperty("slowDownCoefficient");
        viewRect = serializedObject.FindProperty("viewRect");
        scrollDirection = serializedObject.FindProperty("scrollDirection");
        bounceType = serializedObject.FindProperty("bounceType");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(prefabSources, true);

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(localPool);
        EditorGUILayout.PropertyField(isLoop);
        EditorGUILayout.PropertyField(content);
        EditorGUILayout.PropertyField(decelerationRate);
        EditorGUILayout.PropertyField(slowDownCoefficient);
        EditorGUILayout.PropertyField(viewRect);

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(scrollDirection);
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(bounceType);
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(totalCount);

        serializedObject.GetIterator().NextVisible(true);
        serializedObject.ApplyModifiedProperties();
    }

    private SerializedProperty prefabSources;
    private SerializedProperty localPool;
    private SerializedProperty isLoop;
    private SerializedProperty content;
    private SerializedProperty totalCount;
    private SerializedProperty decelerationRate;
    private SerializedProperty slowDownCoefficient;
    private SerializedProperty viewRect;
    private SerializedProperty scrollDirection;
    private SerializedProperty bounceType;
}