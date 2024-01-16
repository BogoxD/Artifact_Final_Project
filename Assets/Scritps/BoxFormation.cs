using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoxFormation : BaseFormation
{
    [SerializeField][Range(0,20)] int width = 3;
    [SerializeField][Range(0,20)] int depth = 3;

    public override IEnumerable<Vector3> EvaluatePositions()
    {
        //initialize formation
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < depth; j++)
            {
                var pos = new Vector3(i, 0, j) * Spread;
                pos += transform.position;
                pos += Get2DNoise(pos);
                
                yield return pos;

            }
        }
    }
   
}
