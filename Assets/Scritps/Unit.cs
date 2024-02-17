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
    [SerializeField] public LayerMask enemy;
    [SerializeField] BoxCollider triggerCheck;

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
            navAgent.enabled = false;
            navAgent.gameObject.SetActive(false);

            if(navAgent.tag == "Skirmisher")
               navAgent.GetComponent<ThrowObject>().enabled = false;
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

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.layer == 7)
        {
            DamageUnit(Random.Range(0,10), collision.gameObject.GetComponent<Unit>());
        }
        else if(collision.gameObject.layer == 6)
        {
            DamageUnit(Random.Range(0, 10), collision.gameObject.GetComponent<Unit>());
        }
    }
    public void DamageUnit(int ammount, Unit unit)
    {
        unit.currentHealth -= ammount;
    }
    public int GetCurrentHealth()
    {
        return currentHealth;
    }
}
