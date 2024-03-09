using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.AI;
using UnityEngine;

public class FormationHandler : MonoBehaviour
{


    private BaseFormation _formation;
    [Header("Steering")]

    [SerializeField] float _avarageSpeed;
    [SerializeField] private bool _hasDestinationReached;
    [SerializeField] Transform _currentTransform;
    [SerializeField] float _circleRadius;
    // Centers of the two circles used for generating the path
    Vector3 _c1 = new();
    Vector3 _c2 = new();
    // Angles where the path separates from c1, and where it joins c2
    float _c1_exitAngle = 0;
    float _c2_enterAngle = 0;
    // How far to go around the circles when generating the points on the path
    float _angleStep = ((5f / 180f) * Mathf.PI);
    //path
    [SerializeField] List<Vector3> _path = new List<Vector3>();

    private Vector3 _targetPosition;
    private Vector3 _targetDir;

    private Vector3 _centerOfMass = new();

    [Header("GameObjects")]
    [SerializeField] private GameObject _unitPrefab;
    private Vector3 _formationPoint;
    [HideInInspector] public List<float> DistancesFromUnitsToPoints;

    [Header("Debug")]
    private int _fartherestUnitIndex;
    private bool _isFighting;
    public Transform c2T;
    public Transform c1T;

    public int SideStart = 0;
    public int SideEnd = 0;

    public BaseFormation Formation
    {
        get
        {
            if (_formation == null) return _formation = GetComponent<BaseFormation>();
            else
                return _formation;
        }
        set => _formation = value;
    }
    private ArmyHandler parentArmy;
    [HideInInspector] public List<Unit> spawnedUnits = new List<Unit>();
    [HideInInspector] public List<Vector3> unitPositions = new List<Vector3>();
    public GameObject[] movingPoints;
    private int _PointIndexToMoveTo = 0;

    int pathIterator = 0;
    private float nextActionTime = 0.0f;
    public float period = 0.1f;
    public int PointIndexToMoveTo
    {
        get { return _PointIndexToMoveTo; }
        set
        {
            if (value != _PointIndexToMoveTo)
            {
                _PointIndexToMoveTo = value;
                _PointIndexToMoveTo = (_PointIndexToMoveTo < -1) ? -1 : _PointIndexToMoveTo;
                _PointIndexToMoveTo = (_PointIndexToMoveTo >= movingPoints.Length)
                    ? movingPoints.Length - 1
                    : _PointIndexToMoveTo;


                Debug.Log("calculateing Steering");
                //setup variables before calculating path
                _targetPosition = movingPoints[_PointIndexToMoveTo].transform.position;
                _targetDir = movingPoints[_PointIndexToMoveTo].transform.forward;

                _currentTransform.position = _centerOfMass;

                _path.Clear();
                pathIterator = 0;
                CalculateSteeringPath(_currentTransform.position, _currentTransform.forward, _targetPosition, _targetDir,
                    _circleRadius); ;
            }
        }
    }
    private void Start()
    {
        movingPoints = GameObject.FindGameObjectsWithTag("Waypoint");

        if (transform.parent)
            parentArmy = transform.parent.GetComponent<ArmyHandler>();

        _currentTransform.position = transform.position;
        _currentTransform.forward = transform.forward;
    }
    void FixedUpdate()
    {
        //Update formation
        SetUpFormation();

        if (Time.time * 0.8 > nextActionTime)
        {
            nextActionTime += period;

            if (PointIndexToMoveTo > -1 && movingPoints.Length > 0 && _path.Count > 2 && pathIterator < _path.Count - 1)
            {
                MoveUnits(_path[pathIterator]);
                pathIterator++;
            }
        }

        _avarageSpeed = ReturnAvarageSpeed(spawnedUnits);
        _currentTransform.forward = GetFormationDirection();
        _centerOfMass = FindCenterOfMass(spawnedUnits);

        //find the furtherst unit from formation position 
        if (DistancesFromUnitsToPoints.Count > 0)
            _fartherestUnitIndex = FindFarUnitIndex();

        //formationTrans is used for calculating the steering path with circles
        if (_hasDestinationReached)
        {
            _currentTransform.position = _centerOfMass;
            _targetPosition = _currentTransform.position;
            _targetDir = _currentTransform.forward;
        }

        _hasDestinationReached = HasReachedDestination(spawnedUnits);
        DistancesFromUnitsToPoints = CalculateDistanceFromUnitToPoint(spawnedUnits, unitPositions);
    }
    void SetUpFormation()
    {
        unitPositions = Formation.EvaluatePositions(_formationPoint).ToList();
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
            _isFighting = agentTmp.GetComponent<FieldOfView>().canSeeEnemy;

            if (agentTmp.enabled)
            {
                //the fartherst unit
                if (i == _fartherestUnitIndex)
                {
                    agentTmp.acceleration = 12f;
                    agentTmp.speed = 6f;
                }
                else
                {
                    agentTmp.speed = 3f;
                    agentTmp.acceleration = 8f;
                }
                if (_isFighting && !agentTmp.GetComponent<ThrowObject>() && agentTmp.GetComponent<FieldOfView>().closestTarget)
                {
                    agentTmp.SetDestination(agentTmp.GetComponent<FieldOfView>().closestTarget.transform.position);
                }
            }
            _hasDestinationReached = HasReachedDestination(spawnedUnits);
        }
    }
    public Vector3 GetTargetPos()
    {
        return _targetPosition;
    }
    public void MoveUnits(Vector3 point)
    {
        for (int i = 0; i < spawnedUnits.Count; i++)
        {
            NavMeshAgent agent = spawnedUnits[i].GetComponent<NavMeshAgent>();

            unitPositions[i] += point;

            agent.SetDestination(unitPositions[i]);
            //COMMENT OUT THE MOVEMENT FOR NOW

            //first row of the formation
            /* if (i < Formation.GetFormationWidth())
             {
                 agent.SetDestination(unitPositions[i]);
             }
             else
             {
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
             */
        }

    }
    public void SetUnitPositions(Vector3 point)
    {
        for (int i = 0; i < unitPositions.Count; i++)
        {
            _formationPoint = point;
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
        float val = DistancesFromUnitsToPoints[0];
        int index = 0;

        for (int i = 1; i < DistancesFromUnitsToPoints.Count; i++)
        {
            if (val < DistancesFromUnitsToPoints[i])
            {
                val = DistancesFromUnitsToPoints[i];
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
            var unit = Instantiate(_unitPrefab, pos, Quaternion.identity, transform);
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

        Vector3 tempCurrentDirection = currentDirection;

        //calculate first circle c1
        Vector3 leftC1 = Vector3.Cross(tempCurrentDirection, transform.up);
        _c1 = currentPosition + (leftC1 * circleRadius);

        //calculate second circle c2
        Vector3 leftC2 = Vector3.Cross(targetDirection.normalized, transform.up);

        _c2 = targetPosition + (leftC2 * circleRadius);

        //debug
        c1T.position = _c1;
        c2T.position = _c2;

        //if the circles overlap scale down the circles
        var distance = Vector3.Distance(_c2, _c1);
        if (distance < circleRadius)
        {
            leftC1 = Vector3.Scale(_c1, -Vector3.one);
            leftC2 = Vector3.Scale(_c2, -Vector3.one);

            _c1 = currentPosition + (leftC1 * circleRadius);
            _c2 = targetPosition + (leftC2 * circleRadius);
        }

        if (leftC1 == RightPerp(tempCurrentDirection))
            SideStart = 1;
        else
            SideStart = 0;

        if (leftC2 == RightPerp(targetDirection))
            SideEnd = 1;
        else
            SideEnd = 0;

        //Calculate the starting circle exit point
        if (SideStart != SideEnd)
        {
            float radius = circleRadius;

            //d is the intersection point between line c1,c2 and line c1_exit, c2_enter 
            float d = (_c2 - _c1).magnitude / 2;
            //angle 1
            float a1 = Mathf.Acos(radius / d);
            //angle 2
            float a2 = Vector3.Angle(_c2, _c1);
            //angle 3
            float a3 = 0;
            if (SideStart == 1 && SideEnd == 0)
                a3 = a2 + a1;
            else
                a3 = a2 - a1;

            _c1_exitAngle = a3;
        }
        else
        {
            if (SideStart == 1)
                _c1_exitAngle = ToAngle(LeftPerp(Subtract(_c2, _c1)).normalized * circleRadius);
            else
                _c1_exitAngle = ToAngle(RightPerp(Subtract(_c2, _c1)).normalized * circleRadius);

        }
        //Calculate the ending cicle entry point
        if (SideStart != SideEnd)
        {
            var radius = circleRadius;
            //d is the intersection point between line c1,c2 and line c1_exit, c2_enter 
            float d = (_c2 - _c1).magnitude / 2;
            //angle 1
            float b1 = Mathf.Acos(radius / d);
            //angle 2
            float b2 = Vector3.Angle(_c1, _c2);
            //angle 3
            float b3 = 0;
            if (SideStart == 1 && SideEnd == 0)
                b3 = b2 + b1;
            else
                b3 = b2 - b1;

            _c2_enterAngle = b3;
        }
        else
        {
            if (SideStart == 1)
                _c2_enterAngle = ToAngle(LeftPerp(Subtract(_c2, _c1)).normalized * circleRadius);
            else
                _c2_enterAngle = ToAngle(RightPerp(Subtract(_c2, _c1)).normalized * circleRadius);
        }

        //calculate points along the path
        GeneratePathArray(SideStart, SideEnd);

    }
    private void GeneratePathArray(int directionStart, int directionEnd)
    {
        //Generate points on the starting cricle
        Vector3 xVec = new Vector3(1, 0, 0);
        Vector3 zVec = new Vector3(0, 0, 1);

        Vector3 startVec = Subtract(_currentTransform.position, _c1);

        float startAngle = ToAngle(startVec);
        float endAngle = _c1_exitAngle;

        if (directionStart == 0) //clockwise
            GeneratePath_Clockwise(startAngle, endAngle, _c1, _path);
        else
            GeneratePath_CounterClockwise(startAngle, endAngle, _c1, _path);

        //Generate points on the ending circle

        Vector3 endVec = Subtract(_targetPosition, _c2);

        float startAngle2 = _c2_enterAngle;
        float endAngle2 = ToAngle(endVec);

        if (directionEnd == 0)
            GeneratePath_Clockwise(startAngle2, endAngle2, _c2, _path);
        else
            GeneratePath_CounterClockwise(startAngle2, endAngle2, _c2, _path);

    }
    private void GeneratePath_Clockwise(float startAngle, float endAngle, Vector3 center, List<Vector3> path)
    {
        if (startAngle > endAngle)
            endAngle += 2 * Mathf.PI;

        float curAngle = startAngle;
        while (curAngle < endAngle)
        {
            var p = new Vector3();
            p.x = center.x + _circleRadius * Mathf.Cos(curAngle);
            p.z = center.z + _circleRadius * Mathf.Sin(curAngle);
            path.Add(p);

            curAngle += _angleStep;
        }

        if (curAngle != endAngle)
        {
            var p = new Vector3();
            p.x = center.x + _circleRadius * Mathf.Cos(endAngle);
            p.z = center.z + _circleRadius * Mathf.Sin(endAngle);
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
            p.x = center.x + _circleRadius * Mathf.Cos(curAngle);
            p.z = center.z + _circleRadius * Mathf.Sin(curAngle);
            path.Add(p);

            curAngle -= _angleStep;
        }

        if (curAngle != endAngle)
        {
            var p = new Vector3();
            p.x = center.x + _circleRadius * Mathf.Cos(endAngle);
            p.z = center.z + _circleRadius * Mathf.Sin(endAngle);
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
    private float ToAngle(Vector3 vector)
    {
        var angle = Mathf.Acos(vector.x / vector.magnitude);

        if (vector.z < 0)
            angle = (2 * Mathf.PI) - angle;

        if (angle < 0)
            angle = (2 * Mathf.PI) + angle;

        return angle;
    }
    private Vector3 Subtract(Vector3 vector1, Vector3 vector2)
    {
        return new Vector3(vector1.x - vector2.x, vector1.y - vector2.y, vector1.z - vector2.z);
    }
    private Vector3 RightPerp(Vector3 vector)
    {
        var result1 = new Vector3(vector.x * -1, vector.y, vector.z);
        return result1;
    }
    private Vector3 LeftPerp(Vector3 vector)
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

        Gizmos.DrawCube(_centerOfMass, Vector3.one);

        Color gizmoreResetColor = Gizmos.color;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(c1T.position, _circleRadius);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(c2T.position, _circleRadius);

        Gizmos.color = gizmoreResetColor;

        //Gizmos.DrawWireSphere();
    }

}
