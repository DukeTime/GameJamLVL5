using UnityEngine;
using System.Collections;

public class AnimOffset : MonoBehaviour {
	
	public string defaultState;
	public bool adjustSpeed = false;

	void Awake()
    {
		Animator anim = GetComponent<Animator>();

		if (anim)
        {
			anim.Play(defaultState, 0, (float)(Random.Range(0,100)) * 0.01f);

			if (adjustSpeed)
				anim.speed = (float)(Random.Range(90,100)) * 0.01f;
		}

		if (GetComponent<Animation>())
        {
			StartCoroutine (WaitThenPlay ());
		}
	}

	IEnumerator WaitThenPlay()
	{
        if (!string.IsNullOrEmpty(defaultState))
        {
            GetComponent<Animation>().Play();
            GetComponent<Animation>()[defaultState].time = Random.value * GetComponent<Animation>().clip.length;
            yield break;
        }
        else
        {
            yield return new WaitForSeconds(Random.value * GetComponent<Animation>().clip.length);
            GetComponent<Animation>().Play();
        }
	}
}
