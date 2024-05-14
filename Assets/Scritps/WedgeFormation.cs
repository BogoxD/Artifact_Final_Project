using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(FormationHandler))]
public class WedgeFormation : BaseFormation
{
    [SerializeField, Range(1,10)] int unitDepth = 3;
    [SerializeField] private bool hollow = false;
    [SerializeField] float Offset = 0;

    public override IEnumerable<Vector3> EvaluatePositions(Vector3 formationPoint)
    {
        var middleOffset = new Vector3(0, 0, unitDepth * 0.5f);

        for(int z = 0; z < unitDepth; z++)
        {
            for(var x = z * -1; x <=z; x++)
            {
                if (hollow && z < unitDepth - 1 && x > z * -1 && x < z) continue;

                var pos = new Vector3(x + (z % 2 == 0 ? 0 : Offset), 0, z * -1);

                pos -= middleOffset;

                pos *= Spread;

                pos += Get2DNoise(pos);

                pos += Vector3.Lerp(pos, formationPoint, 5f);

                pos += transform.position;

                yield return pos;
            }
        }
    }
    public override int GetFormationWidth()
    {
        return 0;
    }
    public override int GetFormationDepth()
    {
        return unitDepth;
    }
}
