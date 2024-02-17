using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class FieldOfView : MonoBehaviour
{
    [Header("Field Of View")]
    public float radiusFov;
    [Range(0,360)]
    public float angleFov;

    public LayerMask targetMask;
    public LayerMask obstructionMask;

    public bool canSeeEnemy;

    public Collider[] enemyTargets;
    public Collider closestTarget;
    private bool once = true;

    private void Start()
    {
        StartCoroutine(FOVRoutine());
    }
    private IEnumerator FOVRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(0.2f);

        while (true)
        {
            yield return wait;
            FieldOfViewCheck();
        }
    }

    private void FieldOfViewCheck()
    {
        enemyTargets = Physics.OverlapSphere(transform.position, radiusFov, targetMask);

        if (enemyTargets.Length != 0)
        {
            for (int i = 0; i < enemyTargets.Length; i++)
            {
                Transform target = enemyTargets[i].transform;

                Vector3 directonToTarget = (target.position - transform.position).normalized;

                if (Vector3.Angle(transform.forward, directonToTarget) < angleFov / 2)
                {
                    float distanceToTarget = Vector3.Distance(transform.position, target.position);

                    if (!Physics.Raycast(transform.position, directonToTarget, distanceToTarget, obstructionMask))
                    {
                        canSeeEnemy = true;
                        if (once)
                        {
                            closestTarget = FindClosestTarget(enemyTargets);
                            once = false; 
                        }
                        if (closestTarget)
                        {
                            if (closestTarget.GetComponent<Unit>().GetCurrentHealth() <= 0)
                            {
                                once = true;
                            }
                            else if (enemyTargets.Length == 1)
                            {
                                closestTarget = enemyTargets[0];
                            }
                        }
                        else
                            closestTarget = FindClosestTarget(enemyTargets);
                    }
                    else
                        canSeeEnemy = false;
                }
                else
                    canSeeEnemy = false;
            }
        }
        else if (canSeeEnemy)
            canSeeEnemy = false;

    }
    IEnumerator Wait(float seconds)
    {
        yield return new WaitForSeconds(seconds);
    }
    Collider FindClosestTarget(Collider[] enemyTargets)
    {
        Collider closestTarget = new Collider();
        Vector3 closestPos = enemyTargets[0].transform.position;

        for (int i = 1; i < enemyTargets.Length; i++)
        {
            if (Vector3.Distance(transform.position, enemyTargets[i].transform.position) > 
                Vector3.Distance(transform.position, closestPos))
            {
                closestTarget = enemyTargets[i];
            }
        }

        return closestTarget;
    }
    Collider PickRandomTarget(Collider[] enemyTargets)
    {
        int randVal = Random.Range(0, enemyTargets.Length);
        return enemyTargets[randVal];
    }
}
