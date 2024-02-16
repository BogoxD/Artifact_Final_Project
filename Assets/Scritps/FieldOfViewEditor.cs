using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(FieldOfView))]
public class FieldOfViewEditor : Editor
{
    private void OnSceneGUI()
    {
        FieldOfView fov = (FieldOfView)target;
        Handles.color = Color.white;
        Handles.DrawWireArc(fov.transform.position, Vector3.up, Vector3.forward, 360, fov.radiusFov);

        Vector3 viewAngle01 = DirectionFromAngle(fov.transform.eulerAngles.y, -fov.angleFov / 2);
        Vector3 viewAngle02 = DirectionFromAngle(fov.transform.eulerAngles.y, fov.angleFov / 2);

        Handles.color = Color.yellow;
        Handles.DrawLine(fov.transform.position, fov.transform.position + viewAngle01 * fov.radiusFov);
        Handles.DrawLine(fov.transform.position, fov.transform.position + viewAngle02 * fov.radiusFov);

        if(fov.canSeeEnemy)
        {
            for (int i = 0; i < fov.enemyTargets.Length; i++)
            {
                Handles.color = Color.green;
                Handles.DrawLine(fov.transform.position, fov.enemyTargets[i].transform.position);
            }
            if (fov.closestTarget)
            {
                Handles.color = Color.red;
                Handles.DrawLine(fov.transform.position, fov.closestTarget.transform.position);
            }

        }
    }

    Vector3 DirectionFromAngle(float eulerY, float angleInDegrees)
    {
        angleInDegrees += eulerY;

        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }
}
