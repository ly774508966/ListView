using UGUI.ListView;
using UnityEditor;

[CustomEditor(typeof(ListView))]
public class ListViewEditor : Editor
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
        elasticity = serializedObject.FindProperty("elasticity");
        rubberScale = serializedObject.FindProperty("rubberScale");
        inertia = serializedObject.FindProperty("inertia");
        movementType = serializedObject.FindProperty("movementType");
        horizontalScrollbarVisibility = serializedObject.FindProperty("horizontalScrollbarVisibility");
        horizontalScrollbar = serializedObject.FindProperty("horizontalScrollbar");
        verticalScrollbarVisibility = serializedObject.FindProperty("verticalScrollbarVisibility");
        verticalScrollbar = serializedObject.FindProperty("verticalScrollbar");
        onValueChanged = serializedObject.FindProperty("onValueChanged");
        enableSnap = serializedObject.FindProperty("enableSnap");
        smoothDumpRate = serializedObject.FindProperty("smoothDumpRate");
        enableDragMove = serializedObject.FindProperty("enableDragMove");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(prefabSources, true);

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(rubberScale);
        EditorGUILayout.PropertyField(localPool);
        EditorGUILayout.PropertyField(isLoop);

        EditorGUILayout.PropertyField(movementType);
        EditorGUILayout.PropertyField(content);
        EditorGUILayout.PropertyField(decelerationRate);
        EditorGUILayout.PropertyField(slowDownCoefficient);
        EditorGUILayout.PropertyField(viewRect);

        EditorGUILayout.PropertyField(inertia);
        if (inertia.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(elasticity);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.PropertyField(scrollDirection);
        EditorGUI.indentLevel++;
        if (scrollDirection.intValue == 0 || scrollDirection.intValue == 1)
        {
            EditorGUILayout.PropertyField(horizontalScrollbarVisibility);
            EditorGUILayout.PropertyField(horizontalScrollbar);
        }
        else
        {
            EditorGUILayout.PropertyField(verticalScrollbarVisibility);
            EditorGUILayout.PropertyField(verticalScrollbar);
        }

        EditorGUI.indentLevel--;
        EditorGUILayout.PropertyField(smoothDumpRate);
        EditorGUILayout.PropertyField(enableDragMove);
        EditorGUILayout.PropertyField(enableSnap);

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(totalCount);
        EditorGUILayout.PropertyField(onValueChanged);


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
    private SerializedProperty elasticity;
    private SerializedProperty rubberScale;
    private SerializedProperty inertia;
    private SerializedProperty movementType;
    private SerializedProperty horizontalScrollbarVisibility;
    private SerializedProperty horizontalScrollbar;
    private SerializedProperty verticalScrollbarVisibility;
    private SerializedProperty verticalScrollbar;
    private SerializedProperty onValueChanged;
    private SerializedProperty enableSnap;
    private SerializedProperty smoothDumpRate;
    private SerializedProperty enableDragMove;
}
