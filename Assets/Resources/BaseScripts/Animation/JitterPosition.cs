using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class JitterPosition : MonoBehaviour
{

    [SerializeField]
    private bool onlyX;

    [SerializeField]
    private float jitterSpeed = 5f;

    [SerializeField]
    private float jitterFrequency = 0.1f;

    private Vector2 currentJitterValue;
	private Vector2 intendedJitterValue;

	private float jitterTimer;

	private Vector2 originalPos;

	public float amount = 0.05f;

	void Start()
	{
		originalPos = transform.localPosition;
	}

	void Update()
    {
		jitterTimer += Time.deltaTime;
		if (jitterTimer > jitterFrequency)
		{
			SetNewIntendedValue();
			jitterTimer = 0f;
		}

		currentJitterValue = Vector2.Lerp(currentJitterValue, intendedJitterValue, Time.deltaTime * jitterSpeed);
		ApplyJitter(currentJitterValue);
	}

    public void Stop()
    {
        enabled = false;
        transform.localPosition = originalPos;
    }

	private void SetNewIntendedValue()
	{
		intendedJitterValue = Random.insideUnitCircle;
	}

	private void ApplyJitter(Vector2 jitterValue) 
	{
		transform.localPosition = new Vector2 (originalPos.x + jitterValue.x * amount, originalPos.y + (onlyX ? 0f : jitterValue.y * amount)); 
	}
}
