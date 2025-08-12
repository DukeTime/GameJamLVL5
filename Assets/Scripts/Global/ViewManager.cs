using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ViewManager : MonoBehaviour
{
    public static ViewManager Instance;

    [SerializeField] private Text titleText;
        
    [SerializeField] private Animator titlePanelAnimator;
    [SerializeField] private Animator fadePanelAnimator;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public IEnumerator FadeIn()
    {
        fadePanelAnimator.SetTrigger("FadeIn");
        yield return StartCoroutine(WaitForAnimation("FadeIn", fadePanelAnimator));
    }
    
    public IEnumerator FadeOut()
    {
        fadePanelAnimator.SetTrigger("FadeOut");
        yield return StartCoroutine(WaitForAnimation("FadeOut", fadePanelAnimator));
    }

    public IEnumerator ShowTitle(string title)
    {
        titlePanelAnimator.SetTrigger("ShowTitle");
        yield return StartCoroutine(WaitForAnimation("ShowTitle", fadePanelAnimator));
    }
    
    IEnumerator WaitForAnimation(string animationName, Animator animator)
    {
        // Ждём, пока анимация начнётся (если она запускается не сразу)
        while (!animator.GetCurrentAnimatorStateInfo(0).IsName(animationName))
        {
            yield return null;
        }
    
        // Ждём, пока анимация завершится
        while (animator.GetCurrentAnimatorStateInfo(0).IsName(animationName) && 
               animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)
        {
            yield return null;
        }
    }
}