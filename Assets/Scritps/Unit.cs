using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Unit : MonoBehaviour
{
    private NavMeshAgent navAgent;
    public Rigidbody rb;

    private void Start()
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
    }
}
