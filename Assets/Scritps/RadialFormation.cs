using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RadialFormation : BaseFormation
{
    [SerializeField] [Range(0,40)] private int ammount = 10;
    [SerializeField] [Range(1,10)]private float radius = 1f;
    [SerializeField][Range(1, 10)] private float rotations = 1f;
    [SerializeField][Range(1,5)] private int rings = 1;
    [SerializeField] private float Offset = 0f;

    public override IEnumerable<Vector3> EvaluatePositions(Vector3 formationPoint)
    {
        var ammountPerRing = ammount / rings;

        for(var i = 0; i < rings; i++)
        {
            for(var j = 0; j < ammountPerRing; j++)
            {
                var angle = j * Mathf.PI * (2 * rotations) / ammountPerRing + (i % 2 != 0 ? Offset : 0);

                var x = Mathf.Cos(angle) * radius;
                var z = Mathf.Sin(angle) * radius;

                var pos = new Vector3(x, 0, z);

                pos += Get2DNoise(pos);

                pos *= Spread;

                pos += transform.position;

                yield return pos;
            }

        }
    }
}
