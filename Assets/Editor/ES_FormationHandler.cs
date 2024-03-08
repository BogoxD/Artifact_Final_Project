using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


[CustomEditor(typeof(FormationHandler))]
[CanEditMultipleObjects]
public class ES_FormationHandler : Editor
{
    SerializedProperty lookAtPoint;

    // Start is called before the first frame update
    void OnEnable()
    {

    }

    public override void OnInspectorGUI()
    {
        FormationHandler FHTarget = (target as FormationHandler);
        DrawDefaultInspector();

        FHTarget.PointIndexToMoveTo =EditorGUILayout.IntSlider(FHTarget.PointIndexToMoveTo ,-1, FHTarget.movingPoints.Length);

        //serializedObject.Update();
        //EditorGUILayout.PropertyField(lookAtPoint);
        //serializedObject.ApplyModifiedProperties();
    }
}
