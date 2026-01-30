using System.Threading;
using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(AudioSource))]
public class StupidAudio : MonoBehaviour
{
    private AudioSource audioSource;
    [SerializeField] private AudioMixer bugMixer;
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.outputAudioMixerGroup = bugMixer.FindMatchingGroups("Bugs")[0];
        audioSource.pitch = Random.Range(0.5f, 1.5f);
        audioSource.panStereo = Random.Range(-1f, 1f);
        float delay = Random.Range(0f, 1f);
        StartCoroutine(DelayedPlay(delay));
        System.Collections.IEnumerator DelayedPlay(float d)
        {
            yield return new WaitForSeconds(d);
            audioSource.Play();
        }
    }

    void Update()
    {
        
    }
}
