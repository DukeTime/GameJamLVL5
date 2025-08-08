using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AnimatingSprite : MonoBehaviour {

    [Header("Frames")]
	public List<Sprite> frames = new List<Sprite>();

    [SerializeField]
    private bool randomizeSprite;

    [Header("Animation")]

    [SerializeField]
    private float animSpeed = 0.033f;

    [SerializeField]
    private float animOffset = 0f;

    [SerializeField]
    private bool randomOffset;

    [SerializeField]
	private bool stopAfterSingleIteration = false;

    [Header("Audio")]
    [SerializeField]
    private string soundId;

    [SerializeField]
    private int soundFrame;

    [Header("Blinking")]
    public List<Sprite> blinkFrames = new List<Sprite>();
    public float blinkRate;
    public float doubleBlinkRate;

    int blinkFrameIndex;
    float blinkTimer;

    float timer;
	SpriteRenderer sR;
	[HideInInspector]
	public int frameIndex = 0;

	private bool stopOnNextFrame;

    void Awake()
    {
        sR = GetComponent<SpriteRenderer>();
    }

	void Start()
    {
        if (randomOffset)
        {
            animOffset = -Random.value * (frames.Count * animSpeed);
        }
		timer = animOffset;
	}

	public void StartAnimatingWithDecrementedIndex()
    {
		frameIndex--;
		StartAnimating ();
	}

	public void StartAnimating()
    {
		this.enabled = true;
		stopOnNextFrame = false;
	}

	public void StopAnimating()
    {
		stopOnNextFrame = true;
	}

    public void StopImmediate()
    {
        Stop();
    }

	public void StartFromBeginning()
    {
		this.enabled = true;
		frameIndex = 0;
	}

    public void IterateFrame()
    {
        if (stopOnNextFrame)
        {
            Stop();
            return;
        }

        timer = 0f;

        if (blinkFrames.Count > 0)
        {
            if (blinkTimer > blinkRate)
            {
                sR.sprite = blinkFrames[blinkFrameIndex];

                if (blinkFrameIndex < blinkFrames.Count -1)
                {
                    blinkFrameIndex++;
                }
                else
                {
                    if (Random.value < doubleBlinkRate)
                    {
                        blinkTimer = blinkRate - 0.1f;
                    }
                    else
                    {
                        blinkTimer = Random.value * -0.5f;
                    }
                    blinkFrameIndex = 0;
                }
                return;
            }
        }

        if (randomizeSprite)
        {
            int randomFrame = Random.Range(0, frames.Count);
            frameIndex = randomFrame;

            sR.sprite = frames[randomFrame];
        }
        else
        {
            frameIndex++;
            if (frameIndex >= frames.Count)
            {
                if (stopAfterSingleIteration)
                {
                    stopAfterSingleIteration = false;
                    this.enabled = false;
                    frameIndex--;
                }
                else {
                    frameIndex = 0;
                }
            }

            sR.sprite = frames[frameIndex];

            if (frameIndex == soundFrame && !string.IsNullOrEmpty(soundId))
            {
                AudioController.Instance.PlaySound(soundId);
            }
        }
    }

	public void Clear()
    {
		this.enabled = false;
		sR.sprite = null;
	}

	private void Stop()
    {
		stopOnNextFrame = false;
		this.enabled = false;
        if (sR != null)
        {
            sR.sprite = frames[0];
        }
	}

	void Update()
    {
        blinkTimer += Time.deltaTime;
        timer += Time.deltaTime;
		if (timer > animSpeed)
        {
            IterateFrame();
		}
	}
}
