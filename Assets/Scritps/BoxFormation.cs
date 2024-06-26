using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(FormationHandler))]
public class BoxFormation : BaseFormation
{
    [SerializeField] [Range(0, 20)] int width = 3;
    [SerializeField] [Range(0, 20)] int depth = 3;
    [SerializeField] bool hollow = false;

    public override IEnumerable<Vector3> EvaluatePositions(Vector3 formationPoint)
    {
        Vector3 middleOffset = new Vector3(width * 0.5f, 0, depth * 0.5f);

        //initialize formation
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < depth; j++)
            {
                if (hollow && i != 0 && i != depth - 1 && j != 0 && j != width - 1) continue;

                var pos = new Vector3(j, 0, -i) * Spread;

                pos += Vector3.Lerp(pos, formationPoint, 5f);

                pos -= middleOffset;

                pos += Get2DNoise(pos);

                pos += transform.position;

                pos += transform.eulerAngles;


                yield return pos;

            }
        }
    }

    public override int GetFormationWidth()
    {
        return width;
    }
    public override int GetFormationDepth()
    {
        return depth;
    }

}
