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
    private Transform parent;

    protected List<Unit> unitsComp = new List<Unit>();
    protected List<Vector3> unitPositions = new List<Vector3>();

    private void Awake()
    {
        parent = new GameObject("Units").transform;
        parent.SetParent(gameObject.transform);
    }

    void Update()
    {
        SetUpFormation();
    }
    void SetUpFormation()
    {
        unitPositions = Formation.EvaluatePositions().ToList();
        //add units to formation
        if (unitPositions.Count > unitsComp.Count)
        {
            var remainingPositions = unitPositions.Skip(unitsComp.Count);
            Spawn(remainingPositions);
        }
        //remove units from formation
        if (unitPositions.Count < unitsComp.Count)
        {
            Kill(unitsComp.Count - unitPositions.Count);
        }
        //move units to positions slots
        for (int i = 0; i < unitsComp.Count; i++)
        {
            NavMeshAgent agentTmp = unitsComp[i].GetNavMeshAgent();
            agentTmp.SetDestination(transform.position + unitPositions[i]);
        }
    }
    void Spawn(IEnumerable<Vector3> positions)
    {
        foreach (Vector3 pos in positions)
        {
            var unit = Instantiate(unitPrefab, transform.position + pos, Quaternion.identity, parent);
            unitsComp.Add(unit.GetComponent<Unit>());
        }
    }
    void Kill(int num)
    {
        for (int i = 0; i < num; i++)
        {
            var unit = unitsComp.Last();
            unitsComp.Remove(unit.GetComponent<Unit>());
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
