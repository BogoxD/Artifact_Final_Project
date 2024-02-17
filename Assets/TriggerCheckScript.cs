using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerCheckScript : MonoBehaviour
{
    Unit unit;

    private void Start()
    {
        unit = transform.parent.GetComponent<Unit>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag == "Spear")
        {
            unit.DamageUnit(Random.Range(20, 30), unit);
        }
    }
}
