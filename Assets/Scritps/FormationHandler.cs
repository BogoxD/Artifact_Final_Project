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
    [SerializeField] Transform _currentDirectionTransform;
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
    private float _unitRadius;

    private int _lastTurnDir;

    [Header("GameObjects")]

    [SerializeField] private GameObject _unitPrefab;
    private Vector3 _formationPoint;
    [HideInInspector] public List<float> DistancesFromUnitsToPoints;

    //Formation State
    private enum FormationState
    {
        Steering,
        MovingToPositions,
        Idle,
        Fighting,
        SlowingDown,
    }
    [SerializeField] private FormationState state = FormationState.Idle;

    [SerializeField] private Unit[,] _spawnedUnits2DArray;
    [Header("Debug")]

    private bool _isFighting;
    [SerializeField] private Transform c2T;
    [SerializeField] private Transform c1T;

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

    //USED FOR COHESION CHECK AND SLOWING DOWN FORMATION
    [HideInInspector] public List<Vector3> fixedUnitPositions = new List<Vector3>();
    public GameObject[] movingPoints;
    private int _PointIndexToMoveTo = 0;
    private int _PointIndexPrevious;

    //iterrate over the path each action time
    int pathIterator = 0;
    private float nextActionTime = 0.5f;
    public float period = 1f;

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

                //set formation state
                state = FormationState.Steering;

                //setup variables before calculating path
                _targetPosition = movingPoints[_PointIndexToMoveTo].transform.position;
                _targetDir = movingPoints[_PointIndexToMoveTo].transform.forward;
                _unitRadius = spawnedUnits[0].GetUnitRadius();

                _currentDirectionTransform.position = _centerOfMass;

                _path.Clear();
                pathIterator = 0;
                CalculateSteeringPath(_currentDirectionTransform.position, _currentDirectionTransform.forward, _targetPosition, _targetDir,
                    _circleRadius);

                //to be fixed
                //Unit[,] array = ConvertTo2DArray(spawnedUnits.ToArray(), Formation.GetFormationDepth(), Formation.GetFormationWidth());
                //FormationBand(_currentDirectionTransform.position, _currentDirectionTransform.forward, array);
            }
        }
    }
    private void Start()
    {
        movingPoints = GameObject.FindGameObjectsWithTag("Waypoint");

        if (transform.parent)
            parentArmy = transform.parent.GetComponent<ArmyHandler>();

        _currentDirectionTransform.position = transform.position;
        _currentDirectionTransform.forward = transform.forward;

    }
    void FixedUpdate()
    {
        //Update formation
        SetUpFormation();

        if (HasFinishedPath())
        {
            state = FormationState.Idle;
            SetUnitPositions(_path[pathIterator]);
        }

        if (Time.time > nextActionTime)
        {
            nextActionTime += period;

            if (PointIndexToMoveTo > -1 && movingPoints.Length > 0 && _path.Count > 2 && pathIterator < _path.Count - 1)
            {
                if (NextPathPos() && state == FormationState.Steering)
                {
                    MoveUnits(_path[pathIterator]);
                    
                    pathIterator++;
                    _PointIndexPrevious = PointIndexToMoveTo;
                }
            }
        }

        _avarageSpeed = ReturnAvarageSpeed(spawnedUnits);
        _currentDirectionTransform.forward = GetFormationDirection();
        _centerOfMass = FindCenterOfMass(spawnedUnits);

        //formationTrans is used for calculating the steering path with circles
        if (_hasDestinationReached)
        {
            _currentDirectionTransform.position = _centerOfMass;
            _targetPosition = _currentDirectionTransform.position;
            _targetDir = _currentDirectionTransform.forward;
        }
        //on movement, move the units from behind to the ones in front
        else
        {
            //slow down formation if units fall behind
            //SlowDownFormation(_spawnedUnits2DArray);

            for (int i = 0; i < spawnedUnits.Count; i++)
            {
                if (i >= Formation.GetFormationDepth())
                {
                    spawnedUnits[i].GetComponent<NavMeshAgent>().SetDestination(spawnedUnits[i - Formation.GetFormationDepth()].transform.position -
                       new Vector3(0, 0, Formation.GetSpread()));
                }
            }
        }

        _hasDestinationReached = HasReachedDestination();
        DistancesFromUnitsToPoints = CalculateDistanceFromUnitToPoint(spawnedUnits, unitPositions);
    }
    void SetUpFormation()
    {
        unitPositions = Formation.EvaluatePositions(_formationPoint).ToList();
        fixedUnitPositions = Formation.EvaluatePositions(_centerOfMass + new Vector3(0, 0, 2)).ToList();

        //add units to formation
        if (unitPositions.Count > spawnedUnits.Count)
        {
            var remainingPositions = unitPositions.Skip(spawnedUnits.Count);
            Spawn(remainingPositions);

            //update array on formation changes
            _spawnedUnits2DArray = ConvertTo2DArray(spawnedUnits.ToArray(), Formation.GetFormationDepth(), Formation.GetFormationWidth());
        }
        //remove units from formation
        if (unitPositions.Count < spawnedUnits.Count)
        {
            Kill(spawnedUnits.Count - unitPositions.Count);

            //update array on formation changes
            _spawnedUnits2DArray = ConvertTo2DArray(spawnedUnits.ToArray(), Formation.GetFormationDepth(), Formation.GetFormationWidth());
        }
        //move units to positions slots
        for (int i = 0; i < spawnedUnits.Count; i++)
        {
            NavMeshAgent agentTmp = spawnedUnits[i].GetNavMeshAgent();
            _isFighting = agentTmp.GetComponent<FieldOfView>().canSeeEnemy;


            if (agentTmp.enabled)
            {
                if (state != FormationState.Steering)
                {
                    MoveUnitsToPositions();
                }

                if (_isFighting && !agentTmp.GetComponent<ThrowObject>() && agentTmp.GetComponent<FieldOfView>().closestTarget)
                {
                    agentTmp.SetDestination(agentTmp.GetComponent<FieldOfView>().closestTarget.transform.position);
                }
            }
            _hasDestinationReached = HasReachedDestination();
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
        }
    }
    public void MoveUnitsToPositions()
    {
        for (int i = 0; i < spawnedUnits.Count; i++)
        {
            NavMeshAgent agentTmp = spawnedUnits[i].GetComponent<NavMeshAgent>();
            agentTmp.SetDestination(unitPositions[i]);
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
    bool HasReachedDestination()
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
    public bool NextPathPos()
    {
        int j = 0;

        for (int i = 0; i < spawnedUnits.Count; i++)
        {
            Unit agent = spawnedUnits[i].GetComponent<Unit>();

            if (agent)
            {
                if (agent.magnitude < 2.5 || _PointIndexPrevious != _PointIndexToMoveTo)
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
    void SetFormationSpeed(float speed, float acceleration)
    {
        for (int i = 0; i < spawnedUnits.Count; i++)
        {
            Unit agentTmp = spawnedUnits[i].GetComponent<Unit>();

            agentTmp.SetAcceleratation(acceleration);
            agentTmp.SetSpeed(speed);
        }
    }
    void SlowDownFormation(Unit[,] spawnedUnits2DArray)
    {
        for (int i = 1; i < spawnedUnits2DArray.GetLength(0); i++)
            for (int j = 0; j < spawnedUnits2DArray.GetLength(1); j++)
            {
                //check distance between formationPosition and
                if (Vector3.Distance(spawnedUnits2DArray[i - 1, j].transform.position, spawnedUnits2DArray[i, j].transform.position) > 4f)
                {
                    //change formation state
                    state = FormationState.SlowingDown;

                    if (0 >= Vector3.Dot(spawnedUnits2DArray[i, j].transform.position.normalized, _currentDirectionTransform.right.normalized))
                    {
                        spawnedUnits2DArray[i, j].SetSpeed(5);
                    }

                    foreach (Unit unitTmp in spawnedUnits)
                    {
                        if (spawnedUnits2DArray[i, j] != unitTmp)
                            unitTmp.SetSpeed(2.5f);
                    }
                }
                else
                {
                    spawnedUnits2DArray[i, j].SetSpeed(3f);
                }
            }
    }
    private static Unit[,] ConvertTo2DArray(Unit[] array, int height, int width)
    {
        Unit[,] output = new Unit[height, width];

        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                output[i, j] = array[i * width + j];
            }
        }
        return output;
    }

    ////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////
    //STEERING FORMATION
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
    private bool HasFinishedPath()
    {
        if (pathIterator == _path.Count - 1)
            return true;
        else
            return false;

    }
    private void FormationBand(Vector3 currentPosition, Vector3 currentDirection, Unit[,] spawnedUnitsArray)
    {
        //set position of first row
        Vector3 rootPos = currentPosition + LeftPerp(currentDirection) * (((Formation.GetFormationDepth() - 1) * _unitRadius * 2) / 2);

        for (int c = 0; c < Formation.GetFormationWidth(); c++)
        {
            spawnedUnitsArray[0, c].transform.position = rootPos + RightPerp(currentDirection) * (2 * c * _unitRadius);
        }

        var prevRowDir = currentDirection.normalized;

        //move the followers in the specified manner
        for (int r = 1; r < Formation.GetFormationDepth(); r++)
        {
            //determine if the row ahead is going left or right
            var leaderPos = spawnedUnitsArray[r - 1, 0].transform.position;
            var unitPos = spawnedUnitsArray[r, 0].transform.position;

            var currentRowDir = Subtract(leaderPos, unitPos).normalized;
            var dotResult = Vector3.Dot(RightPerp(currentRowDir), prevRowDir);

            var turnDir = 0;
            if (dotResult > 0.05)
                turnDir = 1;
            else if (dotResult < -0.05)
                turnDir = -1;
            if (_lastTurnDir != 0 && _lastTurnDir != turnDir)
                turnDir = 0;

            _lastTurnDir = turnDir;

            //if turning left start with leftmost unit
            if (turnDir == -1)
            {
                currentPosition = spawnedUnitsArray[r, 0].transform.position;
                leaderPos = spawnedUnitsArray[r - 1, 0].transform.position;

                var dir = Subtract(currentPosition, leaderPos).normalized * (2 * _unitRadius);
                spawnedUnitsArray[r, 0].transform.position = leaderPos + dir;

                var perpDir = RightPerp(Subtract(leaderPos, currentPosition));

                perpDir = perpDir.normalized * (2 * _unitRadius);

                for (int c = 1; c < Formation.GetFormationWidth(); c++)
                {
                    spawnedUnitsArray[r, c].transform.position = spawnedUnitsArray[r, c - 1].transform.position + perpDir;
                }
            }
            //else sturn at rightmost unit
            else if (turnDir == 1)
            {
                currentPosition = spawnedUnitsArray[r, Formation.GetFormationWidth() - 1].transform.position;
                leaderPos = spawnedUnitsArray[r - 1, Formation.GetFormationWidth() - 1].transform.position;

                var dir = Subtract(currentPosition, leaderPos).normalized * (2 * _unitRadius);
                spawnedUnitsArray[r, Formation.GetFormationWidth() - 1].transform.position = leaderPos + dir;

                var perpDir = LeftPerp(Subtract(leaderPos, currentPosition));
                perpDir = perpDir.normalized * (2 * _unitRadius);

                for (int c = Formation.GetFormationWidth() - 2; c >= 0; c--)
                {
                    spawnedUnitsArray[r, c].transform.position = spawnedUnitsArray[r, c + 1].transform.position + perpDir;
                }
            }
            else
            {
                for (int c = 0; c < Formation.GetFormationWidth(); c++)
                {
                    //get leader pos
                    var curPos = spawnedUnitsArray[r, c].transform.position;
                    var leaderPos1 = spawnedUnitsArray[r - 1, c].transform.position;

                    var dirVec = Subtract(curPos, leaderPos1).normalized * (2 * _unitRadius);
                    spawnedUnitsArray[r, c].transform.position = leaderPos1 + dirVec;
                }
            }
        }
    }
    private void CalculateSteeringPath(Vector3 currentPosition, Vector3 currentDirection, Vector3 targetPosition, Vector3 targetDirection, float circleRadius)
    {
        Vector3 dirVec = (targetPosition - currentPosition).normalized;

        Vector3 tempCurrentDirection = currentDirection;

        //calculate first circle c1
        Vector3 leftC1 = Perpendicular(tempCurrentDirection, dirVec);
        _c1 = currentPosition + (leftC1 * circleRadius);

        //calculate second circle c2
        Vector3 leftC2 = Perpendicular(targetDirection.normalized, -dirVec);

        _c2 = targetPosition + (leftC2 * circleRadius);

        //debug
        c1T.position = _c1;
        c2T.position = _c2;

        //if the circles overlap scale down the circles
        var distance = Vector3.Distance(_c2, _c1);
        if (distance < 2 * circleRadius)
        {
            leftC1 = Vector3.Scale(_c1, -Vector3.one);
            leftC2 = Vector3.Scale(_c2, -Vector3.one);

            _c1 = currentPosition + (leftC1 * circleRadius);
            _c2 = targetPosition + (leftC2 * circleRadius);
        }

        var SideStart = 0;
        var SideEnd = 0;

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
            float a1 = Mathf.Acos(radius / (0.5f * d));
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
                _c1_exitAngle = ToAngle(LeftPerp((Subtract(_c2, _c1)).normalized) * circleRadius);
            else
                _c1_exitAngle = ToAngle(RightPerp((Subtract(_c2, _c1)).normalized) * circleRadius);

        }
        //Calculate the ending cicle entry point
        if (SideStart != SideEnd)
        {
            var radius = circleRadius;
            //d is the intersection point between line c1,c2 and line c1_exit, c2_enter 
            float d = (_c2 - _c1).magnitude / 2;
            //angle 1
            float b1 = Mathf.Acos(radius / (0.5f * d));
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
                _c2_enterAngle = ToAngle(LeftPerp((Subtract(_c2, _c1)).normalized) * circleRadius);
            else
                _c2_enterAngle = ToAngle(RightPerp((Subtract(_c2, _c1)).normalized) * circleRadius);
        }

        //calculate points along the path
        GeneratePathArray(SideStart, SideEnd);

    }
    private void GeneratePathArray(int directionStart, int directionEnd)
    {
        //Generate points on the starting cricle
        Vector3 xVec = new Vector3(1, 0, 0);
        Vector3 zVec = new Vector3(0, 0, 1);

        Vector3 startVec = Subtract(_currentDirectionTransform.position, _c1);

        float startAngle = ToAngle(startVec);
        float endAngle = _c1_exitAngle;

        if (directionStart == 0) //clockwise
            GeneratePath_Clockwise(startAngle, endAngle, _c1, _path);
        else
            GeneratePath_CounterClockwise(startAngle, endAngle, _c1, _path);

        //Generate points on the ending circle

        Vector3 endVec = Subtract(_targetPosition, _c2);

        startAngle = _c2_enterAngle;
        endAngle = ToAngle(endVec);

        if (directionEnd == 0)
            GeneratePath_Clockwise(startAngle, endAngle, _c2, _path);
        else
            GeneratePath_CounterClockwise(startAngle, endAngle, _c2, _path);

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
        var result1 = new Vector3(vector.z, vector.y, vector.x * -1);
        return result1;
    }
    private Vector3 LeftPerp(Vector3 vector)
    {
        var result1 = new Vector3(-1 * vector.z, vector.y, vector.x);
        return result1;
    }
    private void OnDrawGizmos()
    {
        for (int i = 0; i < unitPositions.Count; i++)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(unitPositions[i], 0.5f);
        }

        Gizmos.DrawCube(_centerOfMass, Vector3.one);

        Color gizmoreResetColor = Gizmos.color;

        if (c1T && c2T)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(c1T.position, _circleRadius);
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(c2T.position, _circleRadius);
        }
        Gizmos.color = gizmoreResetColor;

        //Gizmos.DrawWireSphere();
    }

}
