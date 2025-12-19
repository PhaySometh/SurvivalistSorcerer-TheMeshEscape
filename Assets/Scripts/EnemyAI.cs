using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Simple Enemy AI for chase game
/// Enemy patrols the environment and chases the player when detected
/// </summary>
public class EnemyAI : MonoBehaviour
{
    [Header("References")]
    public NavMeshAgent agent;
    public Transform player;

    [Header("Detection Settings")]
    public float sightRange = 30f;
    public float chaseStopDistance = 2f;

    [Header("Patrol Settings")]
    public float walkPointRange = 20f;
    private Vector3 walkPoint;
    private bool walkPointSet = false;

    private enum EnemyState { Patrolling, Chasing }
    private EnemyState currentState = EnemyState.Patrolling;

    private void Awake()
    {
        // Get components
        agent = GetComponent<NavMeshAgent>();
        
        // Find player automatically
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
            else
                player = GameObject.Find("Player").transform;
        }

        // Validate setup
        if (agent == null)
            Debug.LogError("NavMeshAgent component missing on " + gameObject.name);
        if (player == null)
            Debug.LogError("Player not found!");
    }

    private void Update()
    {
        if (player == null || agent == null || !agent.isOnNavMesh)
            return;

        // UPDATED: Always chase the player - no sight range limit!
        // Enemy always knows where player is and chases relentlessly
        ChasePlayer();
    }

    /// <summary>
    /// Enemy patrols around randomly
    /// </summary>
    private void Patrol()
    {
        currentState = EnemyState.Patrolling;

        if (!walkPointSet)
            SearchWalkPoint();

        if (walkPointSet)
        {
            agent.SetDestination(walkPoint);
            
            // Check if reached walk point
            if (!agent.hasPath || agent.remainingDistance <= agent.stoppingDistance)
            {
                if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                    walkPointSet = false;
            }
        }
    }

    /// <summary>
    /// Search for a random patrol point
    /// </summary>
    private void SearchWalkPoint()
    {
        float randomZ = Random.Range(-walkPointRange, walkPointRange);
        float randomX = Random.Range(-walkPointRange, walkPointRange);

        walkPoint = transform.position + new Vector3(randomX, 0f, randomZ);

        // Check if point is on NavMesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(walkPoint, out hit, 5f, NavMesh.AllAreas))
        {
            walkPoint = hit.position;
            walkPointSet = true;
        }
    }

    /// <summary>
    /// Chase the player
    /// </summary>
    private void ChasePlayer()
    {
        currentState = EnemyState.Chasing;
        agent.SetDestination(player.position);
    }

    /// <summary>
    /// Draw gizmos for debugging
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 1, 0, 0.3f); // Yellow
        Gizmos.DrawWireSphere(transform.position, sightRange);
    }
}
