using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.AI;
using UnityEngine;

public class ArmyHandler : MonoBehaviour
{
    private FormationHandler formationHandler;

    [SerializeField] [Range(0, 10)] int armyWidth = 2;
    [SerializeField] [Range(0, 10)] int armyDepth = 2;
    [SerializeField] [Range(8, 20)] int Spread = 8;
    [SerializeField] [Range(2, 10)] int armySpeed = 2;
    [SerializeField] bool hollow = false;
    
    public GameObject formationPrefab;

    protected List<GameObject> spawnedFormations = new List<GameObject>();
    protected List<Vector3> formationPositions = new List<Vector3>();

    private void Update()
    {
        SetupArmy();

    }
    void SetupArmy()
    {
        formationPositions = FormationsPositionsEvaluation().ToList();

        if(formationPositions.Count > spawnedFormations.Count)
        {
            var remainingPositions = formationPositions.Skip(spawnedFormations.Count);
            SpawnFormation(remainingPositions);
        }
        if(formationPositions.Count < spawnedFormations.Count)
        {
            KillFormation(spawnedFormations.Count - formationPositions.Count);
        }
        for(int i = 0; i < spawnedFormations.Count; i++)
        {
            spawnedFormations[i].transform.position = Vector3.MoveTowards(spawnedFormations[i].transform.position, formationPositions[i], 5f * Time.deltaTime);
        }

    }
    public void MoveArmy(Transform point)
    {
        for (int i = 0; i < spawnedFormations.Count; i++)
        {
            //formationPositions[i] += point.position;
            //spawnedFormations[i].transform.position = Vector3.MoveTowards(spawnedFormations[i].transform.position, formationPositions[i] + transform.position, 5f * Time.deltaTime);
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
    public IEnumerable<Vector3> FormationsPositionsEvaluation()
    {
        Vector3 middleOffset = new Vector3(armyWidth * 0.5f, 0, armyDepth * 0.5f);

        for (int i = 0; i < armyWidth; i++)
        {
            for (int j = 0; j < armyDepth; j++)
            {
                if (hollow && i != 0 && i < armyWidth - 1 && j != 0 && j < armyDepth - 1) continue;
                
                var pos = new Vector3(i, 0, j) * Spread;

                pos -= middleOffset;

                yield return pos;

            }

        }
    }
    private void OnDrawGizmos()
    {
        for(int i = 0; i < formationPositions.Count; i++)
        {
            Gizmos.DrawCube(formationPositions[i], Vector3.one);
        }
    }
}
