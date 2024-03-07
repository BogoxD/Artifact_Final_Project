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
    // How far to go around the circles when generating the points on the path
    float angleStep = ((5 / 180) * Mathf.PI);
    //path
    [SerializeField] List<Vector3> path = new List<Vector3>();
    //can calculate path
    bool canCalculatePath = true;

    private Vector3 targetPosition;
    private Vector3 targetDir;

    private Vector3 centerOfMass = new();

    [Header("GameObjects")]
    [SerializeField] private GameObject unitPrefab;
    private Vector3 formationPoint;
    [HideInInspector] public List<float> distancesFromUnitsToPoints;

    [Header("Debug")]
    private int fartherestUnitIndex;
    private bool isFighting;
    public Transform c2T;
    public Transform c1T;

    public int sideStart = 0;
    public int sideEnd = 0;

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
            MoveUnits(movingPoints[0].transform.position);
            targetPosition = movingPoints[0].transform.position;
            targetDir = movingPoints[0].transform.forward;
        }
        else if (move2 && movingPoints.Length > 0)
        {
            MoveUnits(movingPoints[1].transform.position);
            targetPosition = movingPoints[1].transform.position;
            targetDir = movingPoints[1].transform.forward;
        }
        else if (move3 && movingPoints.Length > 0)
        {
            MoveUnits(movingPoints[2].transform.position);
            targetPosition = movingPoints[2].transform.position;
            targetDir = movingPoints[2].transform.forward;
        }
        else
        {
            targetPosition = transform.position;
            targetDir = GetFormationDirection();
            //MoveUnits(targetPosition);
        }
        //find the furtherst unit from formation position 
        if (distancesFromUnitsToPoints.Count > 0)
            fartherestUnitIndex = FindFarUnitIndex();

        //formationTrans is used for calculating the steering path with circles
        if (hasDestinationReached)
        {
            formationTrans.position = centerOfMass;
        }

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
    public void MoveUnits(Vector3 point)
    {

        //calculate steering path when moving
        if (canCalculatePath)
        {
            CalculateSteeringPath(formationTrans.position, formationTrans.forward, targetPosition, targetDir, circleRadius);
            canCalculatePath = false;
        }

        for (int i = 0; i < spawnedUnits.Count; i++)
        {
            NavMeshAgent agent = spawnedUnits[i].GetComponent<NavMeshAgent>();
            //first row of the formation
            if (i < Formation.GetFormationWidth())
            {
                unitPositions[i] += point;

                agent.SetDestination(unitPositions[i]);
            }
            else
            {
                unitPositions[i] += point;

                if (spawnedUnits[i - Formation.GetFormationDepth()].magnitude == 0)
                {
                    agent.SetDestination(unitPositions[i]);
                }
                else
                {
                    //delete Vector3.one later
                    agent.SetDestination(spawnedUnits[i - Formation.GetFormationDepth()].transform.position - Vector3.one);
                }
            }
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
    bool HasReachedDestination(List<Unit> spawnedUnits)
    {
        int j = 0;

        for (int i = 0; i < spawnedUnits.Count; i++)
        {
            Unit agent = spawnedUnits[i].GetComponent<Unit>();

            if (agent)
            {
                if (agent.magnitude == 0)
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
    ////////////////////////////////////////////////////////////
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
        Vector3 dirVec = (targetPosition - currentPosition).normalized;

        Vector3 tempCurrentDirection = currentDirection.normalized;

        //calculate first circle c1
        Vector3 leftC1 = Perpendicular(tempCurrentDirection, dirVec);
        c1 = circleRadius * Vector3.one;
        c1 = leftC1 + currentPosition;

        //calculate second circle c2
        Vector3 leftC2 = Perpendicular(targetDirection.normalized, dirVec * -1);
        c2 = circleRadius * Vector3.one;
        c2 = leftC2 + targetPosition;

        //debug
        c1T.position = c1;
        c2T.position = c2;

        //if the circles overlap scale down the circles
        var distance = Vector3.Distance(c2, c1);
        /*if (distance < 2 * circleRadius)
        {
            leftC1 = Vector3.Scale(c1, -Vector3.one);
            leftC2 = Vector3.Scale(c2, -Vector3.one);

            c1 = currentPosition + leftC1 * circleRadius;
            c2 = targetPosition + leftC2 * circleRadius;
        }*/

        //int sideStart = 0;
        //int sideEnd = 0;

        if (leftC1 == RightPrep(tempCurrentDirection))
            sideStart = 1;
        else
            sideStart = 0;

        if (leftC2 == RightPrep(targetDirection))
            sideEnd = 1;
        else
            sideEnd = 0;

        //Calculate the starting circle exit point
        if (sideStart != sideEnd)
        {
            float radius = circleRadius;

            //d is the intersection point between line c1,c2 and line c1_exit, c2_enter 
            float d = (c2 - c1).magnitude / 2;
            //angle 1
            float a1 = Mathf.Acos(radius / d) / 2;
            //angle 2
            float a2 = Vector3.Angle(c2, c1);
            //angle 3
            float a3 = 0;
            if (sideStart == 1 && sideEnd == 0)
                a3 = a2 + a1;
            else
                a3 = a2 - a1;

            c1_exitAngle = a3;
        }
        //Calculate the ending cicle entry point
        if (sideStart != sideEnd)
        {
            var radius = circleRadius;
            //d is the intersection point between line c1,c2 and line c1_exit, c2_enter 
            float d = (c2 - c1).magnitude / 2;
            //angle 1
            float b1 = Mathf.Acos(radius / d);
            //angle 2
            float b2 = Vector3.Angle(c1, c2);
            //angle 3
            float b3 = 0;
            if (sideStart == 1 && sideEnd == 0)
                b3 = b2 + b1;
            else
                b3 = b2 - b1;

            c2_enterAngle = b3;
        }

        //calculate points along the path
        GeneratePathArray(sideStart, sideEnd);

    }
    private void GeneratePathArray(int directionStart, int directionEnd)
    {
        //Generate points on the starting cricle
        Vector3 xVec = new Vector3(1, 0, 0);
        Vector3 zVec = new Vector3(0, 0, 1);

        float startAngle = Vector3.Angle(formationTrans.position, c1);
        float endAngle = c1_exitAngle;

        if (directionStart == 0) //clockwise
            GeneratePath_Clockwise(startAngle, endAngle, c1, path);
        else
            GeneratePath_CounterClockwise(startAngle, endAngle, c1, path);

        //Generate points on the ending circle
        float startAngle2 = c2_enterAngle;
        float endAngle2 = Vector3.Angle(targetPosition, c2);

        if (directionEnd == 0)
            GeneratePath_Clockwise(startAngle2, endAngle2, c2, path);
        else
            GeneratePath_CounterClockwise(startAngle2, endAngle2, c2, path);

        //set path


    }
    private void GeneratePath_Clockwise(float startAngle, float endAngle, Vector3 center, List<Vector3> path)
    {
        if (startAngle > endAngle)
            endAngle += 2 * Mathf.PI;

        float curAngle = startAngle;
        while (curAngle < endAngle)
        {
            var p = new Vector3();
            p.x = center.x + Formation.GetFormationWidth() * Mathf.Cos(curAngle);
            p.z = center.z + Formation.GetFormationWidth() * Mathf.Sin(curAngle);
            path.Add(p);

            curAngle += angleStep;
        }

        if (curAngle != endAngle)
        {
            var p = new Vector3();
            p.x = center.x + Formation.GetFormationWidth() * Mathf.Cos(endAngle);
            p.z = center.z + Formation.GetFormationWidth() * Mathf.Sin(endAngle);
            path.Add(p);
        }
    }
    private void GeneratePath_CounterClockwise(float startAngle, float endAngle, Vector3 center, List<Vector3> path)
    {
        if (startAngle < endAngle)
            startAngle += 2 * Mathf.PI;

        float curAngle = startAngle;
        while (curAngle > endAngle)
        {
            var p = new Vector3();
            p.x = center.x + Formation.GetFormationWidth() * Mathf.Cos(curAngle);
            p.z = center.z + Formation.GetFormationWidth() * Mathf.Sin(curAngle);
            path.Add(p);

            curAngle -= angleStep;
        }

        if (curAngle != endAngle)
        {
            var p = new Vector3();
            p.x = center.x + Formation.GetFormationWidth() * Mathf.Cos(endAngle);
            p.y = center.y + Formation.GetFormationWidth() * Mathf.Sin(endAngle);
            path.Add(p);
        }
    }

    private Vector3 Perpendicular(Vector3 vector, Vector3 directionVector)
    {
        var result1 = new Vector3(vector.x, vector.y, vector.z * -1);
        var result2 = new Vector3(vector.x * -1, vector.y, vector.z);

        if (Vector3.Dot(result1, directionVector) >= 0)
            return result1;
        else
            return result2;
    }
    private Vector3 RightPrep(Vector3 vector)
    {
        var result1 = new Vector3(vector.x * -1, vector.y, vector.z);
        return result1;
    }
    private Vector3 LeftPrep(Vector3 vector)
    {
        var result1 = new Vector3(vector.x, vector.y, vector.z * -1);
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
