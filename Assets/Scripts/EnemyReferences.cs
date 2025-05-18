using UnityEngine;
using UnityEngine.AI;

public class EnemyReferences : MonoBehaviour
{
    public NavMeshAgent agent;
    public Animator animator;

    public float pathUpdateDelay = 1f;

    public void Init()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
    }
}
