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
    [SerializeField] float avarageSpeed;
    [SerializeField] List<float> distancesFromUnitsToPoints;
    [SerializeField] int fartherestUnitIndex;

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

    public List<Unit> spawnedUnits = new List<Unit>();
    public List<Vector3> unitPositions = new List<Vector3>();
    public GameObject[] movingPoints;

    private void Start()
    {
        movingPoints = GameObject.FindGameObjectsWithTag("Waypoint");
    }
    void Update()
    {
        SetUpFormation();
        if (distancesFromUnitsToPoints.Count > 0)
            fartherestUnitIndex = FindFarUnitIndex();

        avarageSpeed = ReturnAvarageSpeed(spawnedUnits);
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
            move2 = transform.parent.GetComponent<ArmyHandler>().move2;
            move3 = transform.parent.GetComponent<ArmyHandler>().move3;
        }

        distancesFromUnitsToPoints = CalculateDistanceFromUnitToPoint(spawnedUnits, unitPositions);
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

            transform.rotation = Quaternion.LookRotation(transform.forward);
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
    float ReturnAvarageSpeed(List<Unit> spawnedUnits)
    {
        float avarageTotal = 0f;
        float finalTotal = 0f;

        for (int i = 0; i < spawnedUnits.Count; i++)
        {
            avarageTotal += spawnedUnits[i].magnitude;
        }
        finalTotal = avarageTotal / spawnedUnits.Count;

        return finalTotal;
    }
    List<float> CalculateDistanceFromUnitToPoint(List<Unit> spawnedUnits, List<Vector3> unitPositions)
    {
        List<float> distancesFromUnitToPoint = new();

        for (int i = 0; i < unitPositions.Count; i++)
        {
            distancesFromUnitToPoint.Add(Vector3.Distance(spawnedUnits[i].transform.position, unitPositions[i]));
        }
        return distancesFromUnitToPoint;
    }
    int FindFarUnitIndex()
    {
        float val = distancesFromUnitsToPoints[0];
        int index = 0;

        for (int i = 1; i < distancesFromUnitsToPoints.Count; i++)
        {
            if (val < distancesFromUnitsToPoints[i])
            {
                val = distancesFromUnitsToPoints[i];
                index = i;
            }
        }
        return index;
    }
    void FindCenterOfMassPosition()
    {

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
