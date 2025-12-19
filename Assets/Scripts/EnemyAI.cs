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
    public Animator anim;
    public Transform player;

    [Header("Combat Settings")]
    public float attackRange = 1.5f; // Smaller default for small enemies
    public float attackCooldown = 2.0f;
    private float nextAttackTime = 0f;
    private bool isDead = false;

    [Header("Animation Names")]
    public bool useTriggers = true;
    public string idleState = "idle";
    public string runState = "run";
    public string attackStatePrefix = "attack_0";
    public string damageState = "damage";
    public string dieState = "die";

    private string lastAnimationState = "";

    private void Awake()
    {
        // Get components
        agent = GetComponent<NavMeshAgent>();
        if (anim == null) anim = GetComponent<Animator>();
        
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

    private void Start()
    {
        // Subscribe to HealthSystem for feedback animations
        HealthSystem health = GetComponent<HealthSystem>();
        if (health != null)
        {
            health.OnTakeDamage.AddListener(PlayDamageAnimation);
            health.OnDeath.AddListener(PlayDeathAnimation);
        }
    }

    private void Update()
    {
        if (player == null || agent == null || isDead)
            return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (!agent.isOnNavMesh)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 2.0f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
            return;
        }

        if (distanceToPlayer <= attackRange)
        {
            AttackPlayer();
        }
        else
        {
            ChasePlayer();
        }

        // Update movement animation state
        UpdateMovementAnimation();
    }

    private void UpdateMovementAnimation()
    {
        if (anim == null) return;

        float currentSpeed = agent.velocity.magnitude;
        
        if (currentSpeed > 0.1f)
        {
            PlayAnimation(runState);
        }
        else
        {
            PlayAnimation(idleState);
        }
    }

    private void PlayAnimation(string stateName)
    {
        if (lastAnimationState == stateName) return;

        if (useTriggers)
        {
            anim.SetTrigger(stateName);
        }
        else
        {
            anim.CrossFade(stateName, 0.1f);
        }
        
        lastAnimationState = stateName;
    }

    private void AttackPlayer()
    {
        // Stop moving
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        if (Time.time >= nextAttackTime)
        {
            if (anim != null)
            {
                // Randomly pick one of the 3 attack animations
                int attackRoll = Random.Range(1, 4);
                string attackTrigger = attackStatePrefix + attackRoll.ToString();
                
                if (useTriggers) anim.SetTrigger(attackTrigger);
                else anim.CrossFade(attackTrigger, 0.1f);
                
                lastAnimationState = "attacking";
            }
            nextAttackTime = Time.time + attackCooldown;
        }
    }

    private void ChasePlayer()
    {
        agent.isStopped = false;
        agent.SetDestination(player.position);
        
        // Removed the 0.5s delay to make the boss attack instantly when it catches the player
        
        Debug.DrawLine(transform.position, player.position, Color.red);
    }

    private void PlayDamageAnimation()
    {
        if (anim != null && !isDead)
        {
            if (useTriggers) anim.SetTrigger(damageState);
            else anim.CrossFade(damageState, 0.1f);
            
            lastAnimationState = "damage";
        }
    }

    private void PlayDeathAnimation()
    {
        isDead = true;
        if (agent != null) agent.isStopped = true;
        
        if (anim != null)
        {
            if (useTriggers) anim.SetTrigger(dieState);
            else anim.CrossFade(dieState, 0.1f);
            
            lastAnimationState = "dead";
        }
    }


    /// <summary>
    /// Draw gizmos for debugging
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 1, 0, 0.3f); // Yellow
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
