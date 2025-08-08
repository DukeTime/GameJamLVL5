using UnityEngine;
using System.Collections;

public class AnimMethods : MonoBehaviour
{

	public bool AudioMuted { get { return audioMuted; } set { audioMuted = value; } }
	private bool audioMuted;

    void SendMessageUp(string message)
    {
        gameObject.SendMessageUpwards(message, SendMessageOptions.DontRequireReceiver);
    }

    void Destroy()
    {
        Destroy(transform.parent.gameObject);
    }

    void PlaySound(string soundId)
    {
		if (!audioMuted)
		{
			AudioController.Instance.PlaySound (soundId);
		}
    }

    void PlaySoundCrossScene(string soundId)
    {
        if (!audioMuted)
        {
            var sound = AudioController.Instance.PlaySound(soundId);
            DontDestroyOnLoad(sound.gameObject);
        }
    }

    void PlaySoundRandomPitch(string soundId)
    {
		if (!audioMuted)
		{
			AudioController.Instance.PlaySoundWithPitch (soundId, Random.Range (0.9f, 1.1f), 1f);
		}
    }

    void PlaySoundLimited(string soundId)
    {
		if (!audioMuted)
		{
			AudioController.Instance.PlaySoundAtLimitedFrequency (soundId, 0.1f);
		}
    }

    void PlayRandomSound(string soundId)
    {
		if (!audioMuted)
		{
			AudioController.Instance.PlayRandomSound (soundId, noRepeating: true);
		}
    }

    void PlaySoundMuffled(string soundId)
    {
        if (!audioMuted)
        {
            AudioController.Instance.PlaySoundMuffled(soundId, 600f);
        }
    }
}
