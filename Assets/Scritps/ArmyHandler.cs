using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.AI;
using UnityEngine;

public class ArmyHandler : MonoBehaviour
{
    [SerializeField] [Range(0, 10)] int armyWidth = 2;
    [SerializeField] [Range(0, 10)] int armyDepth = 2;
    [SerializeField] [Range(8, 20)] int Spread = 8;
    [SerializeField] [Range(2, 10)] int armySpeed = 2;
    [SerializeField] [Range(0, 1)] float noise = 0f;
    [SerializeField] bool hollow = false;
    [SerializeField] bool squareFormBool = true;
    [SerializeField] bool wedgeFormBool = false;

    float Offset = 0f;

    public UnityEngine.GameObject formationPrefab;

    public List<GameObject> spawnedFormations = new List<GameObject>();
    public List<Vector3> formationPositions = new List<Vector3>();
    public List<Vector3> initialPositions = new List<Vector3>();

    public List<Transform> waypoints;

    private void Update()
    {
        SetupArmy();
    }
    void SetupArmy()
    {
        if (squareFormBool)
            formationPositions = SquareFormation().ToList();
        else if (wedgeFormBool)
            formationPositions = WedgeFormation().ToList();

        if (formationPositions.Count > spawnedFormations.Count)
        {
            var remainingPositions = formationPositions.Skip(spawnedFormations.Count);
            SpawnFormation(remainingPositions);
        }
        if (formationPositions.Count < spawnedFormations.Count)
        {
            KillFormation(spawnedFormations.Count - formationPositions.Count);
        }
        for (int i = 0; i < formationPositions.Count; i++)
        {
            var formHandler = spawnedFormations[i].GetComponent<FormationHandler>();
            formHandler.SetUnitPositions(formationPositions[i]);

            formationPositions[i] = formHandler.FindCenterOfMass(formHandler.spawnedUnits);
            spawnedFormations[i].transform.position = Vector3.MoveTowards(spawnedFormations[i].transform.position, formationPositions[i], 0f).normalized;
            spawnedFormations[i].transform.position = spawnedFormations[i].transform.position.normalized;
        }
    }
    public void MoveArmy(Transform point)
    {
        for (int i = 0; i < spawnedFormations.Count; i++)
        {
            formationPositions[i] += point.position;
            spawnedFormations[i].transform.position = Vector3.MoveTowards(spawnedFormations[i].transform.position, formationPositions[i] + transform.position, 5f * Time.deltaTime);
        }
    }
    void SpawnFormation(IEnumerable<Vector3> positions)
    {
        for(int i = 0; i < positions.Count(); i++)
        {
            var unit = Instantiate(formationPrefab, positions.ToList()[i] + formationPositions[i], Quaternion.identity, transform);
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
    public IEnumerable<Vector3> SquareFormation()
    {
        Vector3 middleOffset = new Vector3(armyWidth * 0.5f, 0, armyDepth * 0.5f);

        for (int i = 0; i < armyWidth; i++)
        {
            for (int j = 0; j < armyDepth; j++)
            {
                if (hollow && i != 0 && i < armyWidth - 1 && j != 0 && j < armyDepth - 1) continue;

                var pos = new Vector3(i, 0, j) * Spread;

                pos += GetArmyNoise(pos);

                pos -= middleOffset;

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

                var pos = new Vector3(x + (z % 2 == 0 ? 0 : Offset), 0, z * -1);

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
        return formationPositions;
    }
    public void SetFormationPosition(Transform trans)
    {
        for (int i = 0; i < formationPositions.Count; i++)
        {
            formationPositions[i] += trans.position;
        }
    }
    public Vector3 GetArmyNoise(Vector3 pos)
    {
        var pNoise = Mathf.PerlinNoise(pos.x * noise, pos.z * noise);

        return new Vector3(pNoise, 0, pNoise);
    }
    private void OnDrawGizmos()
    {
        for (int i = 0; i < formationPositions.Count; i++)
        {
            Gizmos.DrawWireCube(formationPositions[i], Vector3.one);
        }
    }
}
