using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.AI;
using UnityEngine;

public class FormationHandler : MonoBehaviour
{
    private BaseFormation formation;
    [Header("Steering")]
    [SerializeField] bool move1 = false;
    [SerializeField] bool move2 = false;
    [SerializeField] bool move3 = false;
    [SerializeField] float avarageSpeed;
    [SerializeField] private bool hasDestinationReached;
    [SerializeField] Transform formationTrans;
    [SerializeField] float circleRadius;
    // Centers of the two circles used for generating the path
    Vector3 c1 = new();
    Vector3 c2 = new();
    // Angles where the path separates from c1, and where it joins c2
    float c1_exitAngle = 0;
    float c2_enterAngle = 0;

    private Vector3 targetPosition;
    private Vector3 targetDir;

    private Vector3 centerOfMass = new();

    [Header("Prefabs")]
    [SerializeField] private GameObject unitPrefab;
    private Vector3 formationPoint;

    [Header("Debug")]
    [HideInInspector] public List<float> distancesFromUnitsToPoints;
    private int fartherestUnitIndex;
    private bool isFighting;
    public Transform c2T;
    public Transform c1T;

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

        if (transform.parent)
            parentArmy = transform.parent.GetComponent<ArmyHandler>();

        formationTrans.position = transform.position;
    }
    void Update()
    {
        //Update formation
        SetUpFormation();

        avarageSpeed = ReturnAvarageSpeed(spawnedUnits);
        formationTrans.forward = GetFormationDirection();
        centerOfMass = FindCenterOfMass(spawnedUnits);

        if (move1 && movingPoints.Length > 0)
        {
            MoveUnits(movingPoints[0].transform);
            targetPosition = movingPoints[0].transform.position;
            targetDir = movingPoints[0].transform.forward;
        }
        else if (move2 && movingPoints.Length > 0)
        {
            MoveUnits(movingPoints[1].transform);
            targetPosition = movingPoints[1].transform.position;
            targetDir = movingPoints[1].transform.forward;
        }
        else if (move3 && movingPoints.Length > 0)
        {
            MoveUnits(movingPoints[2].transform);
            targetPosition = movingPoints[2].transform.position;
            targetDir = movingPoints[2].transform.forward;
        }
        else
        {
            targetPosition = centerOfMass;
            targetDir = GetFormationDirection();
        }
        if (distancesFromUnitsToPoints.Count > 0)
            fartherestUnitIndex = FindFarUnitIndex();

        hasDestinationReached = HasReachedDestination(spawnedUnits);
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
            isFighting = agentTmp.GetComponent<FieldOfView>().canSeeEnemy;

            if (agentTmp.enabled)
            {
                if (!isFighting)
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
                if (isFighting && !agentTmp.GetComponent<ThrowObject>() && agentTmp.GetComponent<FieldOfView>().closestTarget)
                {
                    agentTmp.SetDestination(agentTmp.GetComponent<FieldOfView>().closestTarget.transform.position);
                }
            }
            hasDestinationReached = HasReachedDestination(spawnedUnits);
        }
    }
    public void MoveUnits(Transform point)
    {
        //formationTrans.position = centerOfMass;

        //calculate steering path when moving
        CalculateSteeringPath(formationTrans.position, formationTrans.forward, targetPosition, targetDir, circleRadius);

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
        foreach (Unit unit in spawnedUnits)
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
            Unit agent = spawnedUnits[i].GetComponent<Unit>();

            if (agent)
            {
               if(agent.magnitude == 0)
                {
                    j++;
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
            unit.transform.forward = gameObject.transform.forward;
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

    ////////////////////////////////////////////////////////////
    //STEERING

    private Vector3 GetFormationDirection()
    {
        Vector3 direction = new Vector3();

        for (int i = 0; i < spawnedUnits.Count; i++)
        {
            direction += spawnedUnits[i].transform.forward;
        }

        direction /= spawnedUnits.Count;
        return direction.normalized;
    }
    private void CalculateSteeringPath(Vector3 currentPosition, Vector3 currentDirection, Vector3 targetPosition, Vector3 targetDirection, float circleRadius)
    {
        Vector3 dirVec = targetPosition - currentPosition;

        //calculate first circle c1
        Vector3 leftC1 = Vector3.Cross(dirVec, Vector3.up).normalized;
        leftC1.z = circleRadius;
        c1 = leftC1 + currentPosition;

        //calculate second circle c2
        Vector3 leftC2 = Vector3.Cross(-dirVec, Vector3.up).normalized;
        leftC2.z = circleRadius;
        c2 = leftC2 + targetPosition;

        c1T.position = c1;
        c2T.position = c2;
    }
    private Vector3 RightPrep(Vector3 vector)
    {
        var result1 = new Vector3(vector.z, 0, -1 * vector.x);
        return result1;
    }
    private Vector3 LeftPrep(Vector3 vector)
    {
        var result1 = new Vector3(-1 * vector.z, 0, vector.x);
        return result1;
    }
    private void OnDrawGizmos()
    {
        for (int i = 0; i < unitPositions.Count; i++)
        {
            Gizmos.DrawSphere(unitPositions[i], 0.5f);
        }

        Gizmos.DrawCube(centerOfMass, Vector3.one);

        Gizmos.DrawWireSphere(c1T.position, circleRadius);
        Gizmos.DrawWireSphere(c2T.position, circleRadius);
    }

}
