using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseFormation : MonoBehaviour
{
    [SerializeField] [Range(2, 4)] protected float Spread = 2f;
    [SerializeField] [Range(0, 1)] float noise = 0;
    public abstract IEnumerable<Vector3> EvaluatePositions();

    public Vector3 Get2DNoise(Vector3 pos)
    {
        var pNoise = Mathf.PerlinNoise(pos.x * noise, pos.z * noise);

        return new Vector3(pNoise, 0, pNoise);
    }

}
