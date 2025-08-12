using UnityEngine;


public class PlayerUI : MonoBehaviour
{
    [SerializeField] private Animator animator;

    public void ShowInteractBtn()
    {
        animator.SetBool("InZone", true);
    }
    
    public void HideInteractBtn()
    {
        animator.SetBool("InZone", false);
    }
}