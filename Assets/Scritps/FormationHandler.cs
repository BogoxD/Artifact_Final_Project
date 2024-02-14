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

    protected List<Unit> spawnedUnits = new List<Unit>();
    protected List<Vector3> unitPositions = new List<Vector3>();

    public GameObject[] movingPoints;
    private void Start()
    {
        movingPoints = GameObject.FindGameObjectsWithTag("Waypoint");
    }
    void Update()
    {
        SetUpFormation();

        if (Input.GetKey(KeyCode.Alpha1) && movingPoints.Length > 0)
        {
            MoveUnits(movingPoints[0].transform);
        }
    }
    void SetUpFormation()
    {
        unitPositions = Formation.EvaluatePositions().ToList();

        //add units to formation
        if (unitPositions.Count > spawnedUnits.Count)
        {
            var remainingPositions = unitPositions.Skip(spawnedUnits.Count);
            Spawn(remainingPositions);
        }
        //remove units from formation
        if (unitPositions.Count < spawnedUnits.Count)
        {
            Kill(spawnedUnits.Count - unitPositions.Count);
        }
        //move units to positions slots
        for (int i = 0; i < spawnedUnits.Count; i++)
        {
            NavMeshAgent agentTmp = spawnedUnits[i].GetNavMeshAgent();
            agentTmp.SetDestination(unitPositions[i]);
        }
    }
    public void MoveUnits(Transform point)
    {
        for (int i = 0; i < spawnedUnits.Count; i++)
        {
            unitPositions[i] += point.position;
            spawnedUnits[i].GetComponent<NavMeshAgent>().SetDestination(unitPositions[i]);
        }
    }
    void Spawn(IEnumerable<Vector3> positions)
    {
        foreach (Vector3 pos in positions)
        {
            var unit = Instantiate(unitPrefab, pos, Quaternion.identity, transform);
            spawnedUnits.Add(unit.GetComponent<Unit>());
        }
    }
    void Kill(int num)
    {
        for (int i = 0; i < num; i++)
        {
            var unit = spawnedUnits.Last();
            spawnedUnits.Remove(unit.GetComponent<Unit>());
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
