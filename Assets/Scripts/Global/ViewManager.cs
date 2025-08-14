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
    
    [SerializeField] private Text newRuneName;
    [SerializeField] private Text newRuneAltName;
    [SerializeField] private Text newRuneDesc;
    [SerializeField] private Animator runeDescAnimator;
    [SerializeField] private GameObject clckToContinue;

    [SerializeField] private Text titleText;
        
    [SerializeField] private Animator titlePanelAnimator;
    [SerializeField] private Animator fadePanelAnimator;
    
    private bool waitingFlag = false;

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

    private IEnumerator DoRuneChoice(Rune rune)
    {
        PlayerStats.ApplyRune(rune);
        ServiceLocator.Current.Get<PlayerController>().Data.UpdateStats();

        FinishRuneChoice();

        yield return new WaitForSeconds(1f);
        
        yield return StartCoroutine(DisplayNew());
        
        waitingFlag = false;
    }

    private IEnumerator DisplayNew()
    {
        CameraShake cameraShake = Camera.main.GetComponent<CameraShake>();
        cameraShake.Shake(0.07f, 2.5f);
        runeDescAnimator.SetBool("Show", true);

        yield return new WaitForSeconds(3f);
        clckToContinue.SetActive(true);
        
        
        while (!Input.GetMouseButtonDown(0))
            yield return null;
        
        clckToContinue.SetActive(false);
        runeDescAnimator.SetBool("Show", false);
        
        yield return new WaitForSeconds(0.3f);
    }
    public IEnumerator StartRuneChoice(List<Rune> runes)
    {
        if (PlayerStats.IsLearned(runes[0].Name))
        {
            option1Txt.text = runes[0].Name;
            option1Desc.text = runes[0].Description;
        }
        else{
            option1Txt.text = runes[0].AltName;
            option1Desc.text = "???";
        }

        if (PlayerStats.IsLearned(runes[1].Name))
        {
            option2Txt.text = runes[1].Name;
            option2Desc.text = runes[1].Description;    
        }
        else{
            option2Txt.text = runes[1].AltName;
            option2Desc.text = "???";    
        }

        if (PlayerStats.IsLearned(runes[2].Name))
        {
            option3Txt.text = runes[2].Name;
            option3Desc.text = runes[2].Description;
        }
        else
        {
            option3Txt.text = runes[2].AltName;
            option3Desc.text = "???";
        }

        option1Btn.onClick.AddListener(() => StartCoroutine(DoRuneChoice(runes[0])));
        option2Btn.onClick.AddListener(() => StartCoroutine(DoRuneChoice(runes[1])));
        option3Btn.onClick.AddListener(() => StartCoroutine(DoRuneChoice(runes[2])));
        
        runeAnimator.SetBool("RunePanelVisible", true);
        
        waitingFlag = true;
        yield return new WaitUntil(() => !waitingFlag);
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