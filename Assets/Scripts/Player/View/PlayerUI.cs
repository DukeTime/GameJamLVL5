using UnityEngine;


public class PlayerUI : MonoBehaviour
{
    [SerializeField] private Animator animator;

    public void ShowInteractBtn()
    {
        animator.SetTrigger("ShowInteractBtn");
    }
    
    public void HideInteractBtn()
    {
        animator.SetTrigger("HideInteractBtn");
    }
}