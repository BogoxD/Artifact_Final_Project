using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.AI;
using UnityEngine;

public class FormationHandler : MonoBehaviour
{
    private BaseFormation formation;
    [SerializeField] bool move1 = false;
    [SerializeField] bool move2 = false;
    [SerializeField] bool move3 = false;

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

        if (move1 && movingPoints.Length > 0)
        {
            MoveUnits(movingPoints[0].transform);
        }
        if (move2 && movingPoints.Length > 0)
        {
            MoveUnits(movingPoints[1].transform);
        }
        if (move3 && movingPoints.Length > 0)
        {
            MoveUnits(movingPoints[2].transform);
        }
        if (transform.parent)
        {
            move1 = transform.parent.GetComponent<ArmyHandler>().move1;
            move1 = transform.parent.GetComponent<ArmyHandler>().move2;
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

            //ignore collision between last spawned unit and the newest spawned unit
            //to optimize later
            for (int i = spawnedUnits.Count - 1; i >= 0; i--)
            {
                Physics.IgnoreCollision(unit.GetComponent<CapsuleCollider>(), spawnedUnits[i].GetComponent<CapsuleCollider>(), true);
            }
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
    //TO IMPLEMENT
    /*private float CalculateAvgFormationSpeed(List<Unit> spawnedUnits)
    {
        float speed = 0f;

        for(int i = 0; i < spawnedUnits.Count; i++)
        {
            speed = spawnedUnits[i].rb.velocity
        }
    }*/
    private void OnDrawGizmos()
    {
        for (int i = 0; i < unitPositions.Count; i++)
        {
            Gizmos.DrawSphere(unitPositions[i], 0.5f);
        }
    }
}
