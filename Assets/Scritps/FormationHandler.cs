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
    [SerializeField] private GameObject unitPrefab;
    private Vector3 formationPoint;

    public List<float> distancesFromUnitsToPoints;
    [SerializeField] private int fartherestUnitIndex;
    [SerializeField] private bool hasDestinationReached;
    private Vector3 centerOfMass = new();

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

    private ArmyHandler parentArmy;
    [HideInInspector] public List<Unit> spawnedUnits = new List<Unit>();
    [HideInInspector] public List<Vector3> unitPositions = new List<Vector3>();
    public GameObject[] movingPoints;

    private void Start()
    {
        movingPoints = GameObject.FindGameObjectsWithTag("Waypoint");

        if(transform.parent)
           parentArmy = transform.parent.GetComponent<ArmyHandler>();
    }
    void Update()
    {
        SetUpFormation();

        avarageSpeed = ReturnAvarageSpeed(spawnedUnits);
        
        centerOfMass = FindCenterOfMass(spawnedUnits);

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

        if (distancesFromUnitsToPoints.Count > 0)
            fartherestUnitIndex = FindFarUnitIndex();

        distancesFromUnitsToPoints = CalculateDistanceFromUnitToPoint(spawnedUnits, unitPositions);
    }
    void SetUpFormation()
    {
        unitPositions = Formation.EvaluatePositions(formationPoint).ToList();

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

            //the fartherst unit
            if (i == fartherestUnitIndex)
            {
                agentTmp.acceleration = 12f;
                agentTmp.speed = 6f;
            }
            else
            {
                agentTmp.speed = 3f;
                agentTmp.acceleration = 8f;
            }

            hasDestinationReached = HasReachedDestination(spawnedUnits);
        }
    }
    public void MoveUnits(Transform point)
    {
        for (int i = 0; i < spawnedUnits.Count; i++)
        {
            unitPositions[i] += point.position;
            NavMeshAgent agent = spawnedUnits[i].GetComponent<NavMeshAgent>();
            agent.SetDestination(unitPositions[i]);
        }
    }
    public void SetUnitPositions(Vector3 point)
    {
        for (int i = 0; i < unitPositions.Count; i++)
        {
            formationPoint = point;
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
    public Vector3 FindCenterOfMass(List<Unit> spawnedUnits)
    {
        var totalX = 0f;
        var totalZ = 0f;
        foreach(Unit unit in spawnedUnits)
        {
            totalX += unit.transform.position.x;
            totalZ += unit.transform.position.z;
        }
        var centerX = totalX / spawnedUnits.Count;
        var centerZ = totalZ / spawnedUnits.Count;

        return new Vector3(centerX, 0, centerZ);
    }
    IEnumerator RigidbodyKinematicOnSpawn(Unit unit)
    {
        if (unit.rb)
        {
            unit.rb.isKinematic = true;

            yield return new WaitForSeconds(3f);

            unit.rb.isKinematic = false;
        }
    }
    bool HasReachedDestination(List<Unit> spawnedUnits)
    {
        int j = 0;

        for (int i = 0; i < spawnedUnits.Count; i++)
        {
            NavMeshAgent agent = spawnedUnits[i].GetComponent<NavMeshAgent>();

            if(!agent.pathPending)
            {
                if(agent.remainingDistance <= agent.stoppingDistance)
                {
                    if(!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                    {
                        j++;
                    }
                }
            }
        }
        if (j == spawnedUnits.Count)
            return true;
        else
            return false;
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

        Gizmos.DrawCube(centerOfMass, Vector3.one);
    }
}
