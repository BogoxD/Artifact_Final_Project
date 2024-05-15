using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.AI;
using UnityEngine;

public class ArmyHandler : MonoBehaviour
{
    [SerializeField] [Range(0, 10)] int armyWidth = 2;
    [SerializeField] [Range(0, 10)] int armyDepth = 2;
    [SerializeField] [Range(0, 20)] int Spread = 8;
    [SerializeField] [Range(-20f, 20f)] float RowOffset = 0f;
    [SerializeField] [Range(2, 10)] int armySpeed = 2;
    [SerializeField] [Range(0, 1)] float noise = 0f;
    [SerializeField] bool hollow = false;
    [SerializeField] bool squareFormBool = true;
    [SerializeField] bool wedgeFormBool = false;
    private Vector3 _formationPoint;

    public GameObject formationPrefab;

    public List<GameObject> spawnedFormations = new List<GameObject>();
    public List<Vector3> formationsPositions = new List<Vector3>();
    public List<Vector3> initialPositions = new List<Vector3>();

    public List<Transform> waypoints;

    private void Update()
    {
        SetupArmy();
    }
    void SetupArmy()
    {
        if (squareFormBool)
            formationsPositions = SquareFormation(_formationPoint).ToList();
        else if (wedgeFormBool)
            formationsPositions = WedgeFormation().ToList();

        if (formationsPositions.Count > spawnedFormations.Count)
        {
            var remainingPositions = formationsPositions.Skip(spawnedFormations.Count);
            SpawnFormation(remainingPositions);
        }
        if (formationsPositions.Count < spawnedFormations.Count)
        {
            KillFormation(spawnedFormations.Count - formationsPositions.Count);
        }
        for (int i = 0; i < formationsPositions.Count; i++)
        {
            var formHandler = spawnedFormations[i].GetComponent<FormationHandler>();
            formHandler.SetUnitPositions(formationsPositions[i]);

            formationsPositions[i] = formHandler.FindCenterOfMass(formHandler.spawnedUnits);
            //SetFormationPosition(formHandler.transform.position);
        }
    }
    public void MoveArmy(Transform point)
    {
        for (int i = 0; i < spawnedFormations.Count; i++)
        {
            formationsPositions[i] += point.position;
            spawnedFormations[i].transform.position = Vector3.MoveTowards(spawnedFormations[i].transform.position, formationsPositions[i] + transform.position, 5f * Time.deltaTime);
        }
    }
    void SpawnFormation(IEnumerable<Vector3> positions)
    {
        foreach (Vector3 pos in positions)
        {
            var unit = Instantiate(formationPrefab, pos, Quaternion.identity, transform);
            spawnedFormations.Add(unit);
        }
    }
    void KillFormation(int num)
    {
        for (int i = 0; i < num; i++)
        {
            var unit = spawnedFormations.Last();
            spawnedFormations.Remove(unit);
            Destroy(unit);
        }
    }
    public IEnumerable<Vector3> SquareFormation(Vector3 _formationPoint)
    {
        Vector3 middleOffset = new Vector3(armyWidth * 0.5f, 0, armyDepth * 0.5f);

        for (int i = 0; i < armyWidth; i++)
        {
            for (int j = 0; j < armyDepth; j++)
            {
                if (hollow && i != 0 && i < armyWidth - 1 && j != 0 && j < armyDepth - 1) continue;

                var pos = new Vector3(i + (j % 2 == 0 ? 0 : RowOffset), 0, j) * Spread;

                pos += Vector3.Lerp(pos, _formationPoint, 5f);

                pos -= middleOffset;

                pos += GetArmyNoise(pos);

                pos += transform.position;

                yield return pos;

            }

        }
    }
    public IEnumerable<Vector3> WedgeFormation()
    {
        var middleOffset = new Vector3(0, 0, armyDepth * 0.5f);

        for (int z = 0; z < armyDepth; z++)
        {
            for (var x = z * -1; x <= z; x++)
            {
                if (hollow && z < armyDepth - 1 && x > z * -1 && x < z) continue;

                var pos = new Vector3(x + (z % 2 == 0 ? 0 : RowOffset), 0, z * -1);

                pos -= middleOffset;

                pos += GetArmyNoise(pos);

                pos *= Spread;

                pos += transform.position;

                yield return pos;
            }
        }
    }
    public IEnumerable<Vector3> GetFormationPositions()
    {
        return formationsPositions;
    }
    public void SetFormationPosition(Vector3 position)
    {
        for (int i = 0; i < spawnedFormations.Count; i++)
        {
            _formationPoint = position;
        }
    }
    public Vector3 GetArmyNoise(Vector3 pos)
    {
        var pNoise = Mathf.PerlinNoise(pos.x * noise, pos.z * noise);

        return new Vector3(pNoise, 0, pNoise);
    }
    private void OnDrawGizmos()
    {
        for (int i = 0; i < formationsPositions.Count; i++)
        {
            Gizmos.DrawWireCube(formationsPositions[i], Vector3.one);
        }
    }
}
