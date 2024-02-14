using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.AI;
using UnityEngine;

public class FormationHandler : MonoBehaviour
{
    private BaseFormation formation;

    public BaseFormation Formation
    {
        get
        {
            if (formation == null) return formation = GetComponent<BaseFormation>();
            else
                return formation;
        }
        set => formation = value;
    }

    [SerializeField] private GameObject unitPrefab;

    public List<Unit> units = new List<Unit>();
    protected List<Vector3> unitPositions = new List<Vector3>();
    void Update()
    {
        SetUpFormation();
    }
    void SetUpFormation()
    {
        unitPositions = Formation.EvaluatePositions().ToList();
        //add units to formation
        if (unitPositions.Count > units.Count)
        {
            var remainingPositions = unitPositions.Skip(units.Count);
            Spawn(remainingPositions);
        }
        //remove units from formation
        if (unitPositions.Count < units.Count)
        {
            Kill(units.Count - unitPositions.Count);
        }
        //move units to positions slots
        for (int i = 0; i < units.Count; i++)
        {
            NavMeshAgent agentTmp = units[i].GetNavMeshAgent();
            agentTmp.SetDestination(transform.position + unitPositions[i]);
        }
    }
    void Spawn(IEnumerable<Vector3> positions)
    {
        foreach (Vector3 pos in positions)
        {
            var unit = Instantiate(unitPrefab, transform.position + pos, Quaternion.identity, transform);
            units.Add(unit.GetComponent<Unit>());
        }
    }
    void Kill(int num)
    {
        for (int i = 0; i < num; i++)
        {
            var unit = units.Last();
            units.Remove(unit.GetComponent<Unit>());
            Destroy(unit.gameObject);
        }
    }
    private void OnDrawGizmos()
    {
        for (int i = 0; i < unitPositions.Count; i++)
        {
            Gizmos.DrawSphere(unitPositions[i], 0.5f);
        }
    }
}
