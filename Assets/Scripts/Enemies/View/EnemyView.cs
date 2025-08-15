using System;
using System.Collections;
using UnityEngine;

public class EnemyView: MonoBehaviour
{
    [SerializeField] private Animator animator;
    
    public IEnumerator Death()
    {
        animator.SetTrigger("Death");
        yield return new WaitForSeconds(1);
        Destroy(gameObject);
    }
}