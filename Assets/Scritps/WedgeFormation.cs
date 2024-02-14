using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WedgeFormation : BaseFormation
{
    [SerializeField, Range(1,10)] int unitDepth = 3;
    [SerializeField] private bool hollow = false;
    [SerializeField] float Offset = 0;

    public override IEnumerable<Vector3> EvaluatePositions()
    {
        var middleOffset = new Vector3(0, 0, unitDepth * 0.5f);

        for(int z = 0; z < unitDepth; z++)
        {
            for(var x = z * -1; x <=z; x++)
            {
                if (hollow && z < unitDepth - 1 && x > z * -1 && x < z) continue;

                var pos = new Vector3(x + (z % 2 == 0 ? 0 : Offset), 0, z * -1);

                pos += transform.position;

                pos -= middleOffset;

                pos += Get2DNoise(pos);

                pos *= Spread;

                yield return pos;
            }
        }
    }

    //first iterration
    /*
    public GameObject prefab;
    int rows = 3;
    int spread = 2;
    public void WedgeForm()
    {
        Vector3 targetPosition = new Vector3(-1, 10, 0);
        float yOffset = -1f;
        float xOffset = 1f;
        float rowOffset = -0.5f;

        for (int i = 1; i <= rows; i++)
        {
            for (int j = 0; j < i; j++)
            {
                GameObject instance = Instantiate(prefab, transform);
                targetPosition = new Vector3(targetPosition.x + xOffset, 10,targetPosition.z);
                instance.transform.position = targetPosition;

            }

            //offset new row
            targetPosition = new Vector3((rowOffset * i) - 1.0f, 10, targetPosition.z + yOffset);

        }

    }*/

}
