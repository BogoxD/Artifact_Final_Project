using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoxFormation : BaseFormation
{
    [SerializeField][Range(0,20)] int width = 3;
    [SerializeField][Range(0,20)] int depth = 3;
    [SerializeField] bool hollow = false;
    
    public override IEnumerable<Vector3> EvaluatePositions()
    {
        Vector3 middleOffset = new Vector3(width * 0.5f, 0, depth * 0.5f);

        //initialize formation
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < depth; j++)
            {
                if (hollow && i != 0 && i != width - 1 && j != 0 && j != depth - 1) continue;

                var pos = new Vector3(i, 0, j) * Spread;

                pos -= middleOffset;

                pos += Get2DNoise(pos);

                yield return pos;

            }
        }
    }
   
}
