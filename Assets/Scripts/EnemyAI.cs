using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Simple Enemy AI for chase game
/// FIXED: Added animator safety checks to prevent console spam errors
/// </summary>
public class EnemyAI : MonoBehaviour
{
    [Header("References")]
    public NavMeshAgent agent;
    public Animator anim;
    public Transform player;

    [Header("Combat Settings")]
    public float attackRange = 1.5f;
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
    
    // Cache valid parameters to avoid repeated lookups
    private HashSet<string> validTriggers = new HashSet<string>();
    private HashSet<int> validStateHashes = new HashSet<int>();
    private bool animatorValidated = false;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (anim == null) anim = GetComponent<Animator>();
        
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
            else
            {
                GameObject foundPlayer = GameObject.Find("Player");
                if (foundPlayer != null) player = foundPlayer.transform;
            }
        }

        if (agent == null)
            Debug.LogError($"NavMeshAgent missing on {gameObject.name}");
    }

    private void Start()
    {
        // Validate animator parameters ONCE at start
        ValidateAnimator();
        
        HealthSystem health = GetComponent<HealthSystem>();
        if (health != null)
        {
            health.OnTakeDamage.AddListener(PlayDamageAnimation);
            health.OnDeath.AddListener(PlayDeathAnimation);
        }
    }
    
    /// <summary>
    /// Cache all valid animator parameters and states at startup
    /// This prevents repeated lookups and console spam
    /// </summary>
    private void ValidateAnimator()
    {
        if (anim == null || anim.runtimeAnimatorController == null)
        {
            animatorValidated = false;
            return;
        }
        
        // Cache all trigger parameters
        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            if (param.type == AnimatorControllerParameterType.Trigger)
            {
                validTriggers.Add(param.name);
            }
        }
        
        // Cache valid state hashes for layer 0
        string[] statesToCheck = { idleState, runState, damageState, dieState, 
                                   attackStatePrefix + "1", attackStatePrefix + "2", attackStatePrefix + "3" };
        
        foreach (string stateName in statesToCheck)
        {
            int hash = Animator.StringToHash(stateName);
            if (anim.HasState(0, hash))
            {
                validStateHashes.Add(hash);
            }
        }
        
        animatorValidated = true;
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

        UpdateMovementAnimation();
    }

    private void UpdateMovementAnimation()
    {
        if (anim == null || !animatorValidated) return;

        float currentSpeed = agent.velocity.magnitude;
        
        if (currentSpeed > 0.1f)
        {
            SafePlayAnimation(runState);
        }
        else
        {
            SafePlayAnimation(idleState);
        }
    }

    /// <summary>
    /// Safely play animation - NO CONSOLE ERRORS
    /// Only triggers animations that have been validated to exist
    /// </summary>
    private void SafePlayAnimation(string stateName)
    {
        if (anim == null || string.IsNullOrEmpty(stateName)) return;
        if (lastAnimationState == stateName) return;

        if (useTriggers)
        {
            // Only call SetTrigger if we KNOW this trigger exists
            if (validTriggers.Contains(stateName))
            {
                anim.SetTrigger(stateName);
                lastAnimationState = stateName;
            }
        }
        else
        {
            // Only call CrossFade if we KNOW this state exists
            int hash = Animator.StringToHash(stateName);
            if (validStateHashes.Contains(hash))
            {
                anim.CrossFade(hash, 0.1f, 0); // Layer 0 explicitly - FIXES Invalid Layer Index error
                lastAnimationState = stateName;
            }
        }
    }

    private void AttackPlayer()
    {
        if (agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        if (Time.time >= nextAttackTime)
        {
            if (anim != null && animatorValidated)
            {
                int attackRoll = Random.Range(1, 4);
                string attackTrigger = attackStatePrefix + attackRoll.ToString();
                
                if (useTriggers && validTriggers.Contains(attackTrigger))
                {
                    anim.SetTrigger(attackTrigger);
                    lastAnimationState = "attacking";
                }
                else if (!useTriggers)
                {
                    int hash = Animator.StringToHash(attackTrigger);
                    if (validStateHashes.Contains(hash))
                    {
                        anim.CrossFade(hash, 0.1f, 0); // Layer 0 explicitly
                        lastAnimationState = "attacking";
                    }
                }
            }
            nextAttackTime = Time.time + attackCooldown;
        }
    }

    private void ChasePlayer()
    {
        if (agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }
    }

    private void PlayDamageAnimation()
    {
        if (anim == null || isDead || !animatorValidated) return;
        
        if (useTriggers && validTriggers.Contains(damageState))
        {
            anim.SetTrigger(damageState);
            lastAnimationState = "damage";
        }
        else if (!useTriggers)
        {
            int hash = Animator.StringToHash(damageState);
            if (validStateHashes.Contains(hash))
            {
                anim.CrossFade(hash, 0.1f, 0);
                lastAnimationState = "damage";
            }
        }
    }

    private void PlayDeathAnimation()
    {
        isDead = true;
        
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }
        
        if (anim == null || !animatorValidated) return;
        
        if (useTriggers && validTriggers.Contains(dieState))
        {
            anim.SetTrigger(dieState);
            lastAnimationState = "dead";
        }
        else if (!useTriggers)
        {
            int hash = Animator.StringToHash(dieState);
            if (validStateHashes.Contains(hash))
            {
                anim.CrossFade(hash, 0.1f, 0);
                lastAnimationState = "dead";
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 1, 0, 0.3f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
