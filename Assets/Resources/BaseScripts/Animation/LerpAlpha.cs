using UnityEngine;
using System.Collections;

public class LerpAlpha : MonoBehaviour {

	public float speed = 2.5f;
	public float intendedAlpha;
	SpriteRenderer sR;
	TextMesh tM;
	
	void Awake ()
    {
		sR = GetComponent<SpriteRenderer>();
		tM = GetComponent<TextMesh>();
	}
	
	void Update()
    {
		Color current = GetColor ();
		Color c = new Color(current.r, current.g, current.b, Mathf.Lerp(current.a, intendedAlpha, Time.deltaTime * speed));
		SetColor (c);
	}

	public Color GetColor()
	{
		if (sR) {
			return sR.color;
		}
		else if (tM) {
			return tM.color;
		}

		return Color.black;
	}

	public void SetColor (Color c)
	{
		if (sR) {
			sR.color = c;
		}
		else if (tM) {
			tM.color = c;
		}
	}
}
