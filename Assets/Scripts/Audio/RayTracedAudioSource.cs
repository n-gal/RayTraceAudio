using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class RayTracedAudioSource : MonoBehaviour
{
    [SerializeField] private AudioClip m_audioClip;
    [SerializeField] public bool m_loop = false;
    [SerializeField] public float m_baseAudioVolume = 1f;
    [SerializeField] public float m_minDistance = 1f;
    [SerializeField] public float m_maxDistance = 500f;
    [HideInInspector] public List<Vector3> m_heardPositions = new List<Vector3>();
    [HideInInspector] public Vector3 m_virtualPosition;
    private AudioSource m_audioSource;
    private AudioLowPassFilter m_audioLowPassFilter;
    void Awake()
    {
        m_audioSource = gameObject.AddComponent(typeof(AudioSource)) as AudioSource;
        m_audioLowPassFilter = gameObject.AddComponent(typeof(AudioLowPassFilter)) as AudioLowPassFilter;
        m_virtualPosition = transform.position;
        m_audioSource.clip = m_audioClip;
        m_audioSource.volume = m_baseAudioVolume;
        m_audioSource.minDistance = m_minDistance;
        m_audioSource.maxDistance = m_maxDistance;
        m_audioSource.loop = m_loop;
        m_audioSource.Play();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, m_minDistance);
        Gizmos.DrawWireSphere(transform.position, m_maxDistance);
    }

    public void SetVolume(float volume)
    {
        m_audioSource.volume = volume;
    }

    public void SetPanning(float panning)
    {
        m_audioSource.panStereo = panning;
    }

    public void SetLowPassCutoff(float cutoffFrequency)
    {
        m_audioLowPassFilter.cutoffFrequency = cutoffFrequency;
    }

    public void ClearHearPoints()
    {
        m_heardPositions.Clear();
    }
}
