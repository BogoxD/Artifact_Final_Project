using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Unit : MonoBehaviour
{
    private NavMeshAgent navAgent;
    public Rigidbody rb;
    [SerializeField] public Vector3 velocity;
    [SerializeField] public float magnitude;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public NavMeshAgent GetNavMeshAgent()
    {
        navAgent = GetComponent<NavMeshAgent>();
        return navAgent;
    }

    private void Update()
    {
        velocity = navAgent.velocity;
        magnitude = navAgent.velocity.magnitude;

        
        if(magnitude <= 0)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, transform.parent.rotation, Time.deltaTime);
        }
    }
}
