using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ViewManager : MonoBehaviour
{
    public static ViewManager Instance;
    
    
    [SerializeField] private Text option1Desc;
    [SerializeField] private Text option2Desc;
    [SerializeField] private Text option3Desc;
    [SerializeField] private Text option1Txt;
    [SerializeField] private Text option2Txt;
    [SerializeField] private Text option3Txt;
    [SerializeField] private Button option1Btn;
    [SerializeField] private Button option2Btn;
    [SerializeField] private Button option3Btn;
    [SerializeField] private Animator runeAnimator;

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

    private void DoRuneChoice(Rune rune)
    {
        PlayerStats.ApplyRune(rune);
        ServiceLocator.Current.Get<PlayerController>().Data.UpdateStats();
        
        FinishRuneChoice();
    }

    private IEnumerator Display()
    {
        yield return null;
    }
    public void StartRuneChoice(List<Rune> runes)
    {
        option1Txt.text = runes[0].Name;
        option2Txt.text = runes[1].Name;
        option3Txt.text = runes[2].Name;
        option1Desc.text = runes[0].Description;
        option2Desc.text = runes[1].Description;
        option3Desc.text = runes[2].Description;
        option1Btn.onClick.AddListener(() => DoRuneChoice(runes[0]));
        option2Btn.onClick.AddListener(() => DoRuneChoice(runes[1]));
        option3Btn.onClick.AddListener(() => DoRuneChoice(runes[2]));
        
        runeAnimator.SetBool("RunePanelVisible", true);
    }
    
    public void FinishRuneChoice()
    {
        runeAnimator.SetBool("RunePanelVisible", false);
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