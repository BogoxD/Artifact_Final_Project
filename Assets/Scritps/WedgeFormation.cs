using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WedgeFormation : MonoBehaviour
{
    public GameObject prefab;
    [SerializeField, Range(1, 5)] int rows = 3;
    [SerializeField] int spread = 2;

    private void Start()
    {
        WedgeForm();
    }
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

    }
}
