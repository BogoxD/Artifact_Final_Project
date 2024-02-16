using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Unit : MonoBehaviour
{
    private NavMeshAgent navAgent;
    [SerializeField] public int maxHealth = 100;
    private int currentHealth;
    [HideInInspector] public Rigidbody rb;

    [Header("Nav Agent Parameters")]
    [SerializeField] public Vector3 velocity;
    [SerializeField] public float magnitude;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        currentHealth = maxHealth;
    }

    public NavMeshAgent GetNavMeshAgent()
    {
        navAgent = GetComponent<NavMeshAgent>();
        return navAgent;
    }

    private void Update()
    {
        if(currentHealth <= 0)
        {
            gameObject.SetActive(false);
        }

        if (navAgent)
        {
            velocity = navAgent.velocity;
            magnitude = navAgent.velocity.magnitude;


            //Change Avoidance Type if moving
            navAgent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            navAgent.avoidancePriority = 20;

            //Change Avoidance Type if stationary
            if (velocity.x <= 0 && velocity.z <= 0)
            {
                //make unit rotate twords parent forward vector
                transform.rotation = Quaternion.Lerp(transform.rotation, transform.parent.rotation, Time.deltaTime);

                navAgent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
                navAgent.avoidancePriority = 90;
            }
        }
    }
}
