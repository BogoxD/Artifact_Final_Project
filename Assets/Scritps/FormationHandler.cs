using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    [SerializeField] private float unitSpeed = 2f;
    private Transform parent;

    private readonly List<GameObject> spawnedUnits = new List<GameObject>();
    private List<Vector3> unitPositions = new List<Vector3>();

    private void Awake()
    {

        parent = new GameObject("Units").transform;
        parent.SetParent(gameObject.transform);
    }

    void Update()
    {
        SetUpFormation();

        if (Input.GetKey(KeyCode.K))
        {
            MoveFormation();
        }
    }
    void Spawn(IEnumerable<Vector3> positions)
    {
        foreach (Vector3 pos in positions)
        {
            var unit = Instantiate(unitPrefab, transform.position + pos, Quaternion.identity, parent);
            spawnedUnits.Add(unit);
        }
    }
    void Kill(int num)
    {
        for (int i = 0; i < num; i++)
        {
            var unit = spawnedUnits.Last();
            spawnedUnits.Remove(unit);
            Destroy(unit.gameObject);
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
            spawnedUnits[i].transform.position = Vector3.MoveTowards(spawnedUnits[i].transform.position, transform.position + unitPositions[i], unitSpeed * Time.deltaTime);
        }
    }
    void MoveFormation()
    {

        for (int i = 0; i < unitPositions.Count; i++)
        {
            unitPositions[i] += new Vector3(10, 0, 10);
        }
        
        for (int i = 0; i < spawnedUnits.Count; i++)
        {
            spawnedUnits[i].transform.position = Vector3.MoveTowards(spawnedUnits[i].transform.position, transform.position + unitPositions[i], unitSpeed * Time.deltaTime);
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
