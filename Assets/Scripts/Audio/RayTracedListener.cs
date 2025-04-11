using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEngine.UI.Image;

public class RayTracedListener : MonoBehaviour
{
    [SerializeField] private AnimationCurve m_soundFalloff;
    [SerializeField] private AnimationCurve m_soundFalloffDampening;
    [SerializeField] private AnimationCurve m_soundObstructionDampening;
    private List<RayTracedAudioSource> m_sources = new List<RayTracedAudioSource>(); 

    void Awake()
    {
        m_sources = new List<RayTracedAudioSource>(Resources.FindObjectsOfTypeAll<RayTracedAudioSource>());
    }


    void Update()
    {
        DoSoundFalloff();
    }

    void DoSoundFalloff()
    {
        foreach (RayTracedAudioSource source in m_sources)
        {
            DoSoundDirectionality(source);

            float maxObstruction = 4f;
            float distance = Vector3.Distance(source.m_virtualPosition, transform.position);
            float maxDist = source.m_maxDistance;
            float minDist = source.m_minDistance;

            float distanceThroughWalls = GetSoundObstruction(source.m_virtualPosition, distance);


            float obstructionFactor = Mathf.InverseLerp(maxObstruction, 0f, distanceThroughWalls);
            print(obstructionFactor);
            obstructionFactor = m_soundObstructionDampening.Evaluate(obstructionFactor);

            float clampedDistance = Mathf.Max(distance, 1f);

            float remappedDist = Mathf.InverseLerp(1f, maxDist, clampedDistance);

            source.SetVolume(source.m_baseAudioVolume * (m_soundFalloff.Evaluate(remappedDist) * obstructionFactor));

            float remappedCutoff = Mathf.Lerp(10, 22000, m_soundFalloffDampening.Evaluate(remappedDist)) * obstructionFactor/2;

            source.SetLowPassCutoff(remappedCutoff);
        }
    }

    float GetSoundObstruction(Vector3 targetAudioSource, float audioSourceDistance)
    {
        Vector3 direction = (targetAudioSource - transform.position).normalized;

        RaycastHit[] entries = new RaycastHit[8],
                        exits = new RaycastHit[8];

        List<RaycastHit> intersections = new List<RaycastHit>(16);

        // Does the ray intersect any objects excluding the player layer
        float radius = 0f;
        float distance = audioSourceDistance;
        float distanceThroughWall = 0;
        Vector3 startRayTarget = transform.position;

        BidirectionalRaycastNonAlloc(transform.position, radius, direction, distance, ref entries, ref exits, ref intersections);

        //Debug.DrawLine(transform.position, targetAudioSource.position);

        for (int i = 0; i < exits.Length - 1; i++)
        {
            if (exits[i].normal != Vector3.zero)
            {
                distanceThroughWall += Vector3.Distance(entries[i].point, exits[i].point);
            }
        }
        return distanceThroughWall;
    }


    public void BidirectionalRaycastNonAlloc(Vector3 origin, float radius, Vector3 direction, float length, ref RaycastHit[] entries, ref RaycastHit[] exits, ref List<RaycastHit> hits)
    {
        hits.Clear();
        int hitNumber1, hitNumber2;
        direction.Normalize();
        if (radius <= 0f)
        {
            hitNumber1 = Physics.RaycastNonAlloc(origin, direction, entries, length);
            hitNumber2 = Physics.RaycastNonAlloc(origin + (direction * length), -direction, exits, length);
        }
        else
        {
            hitNumber1 = Physics.SphereCastNonAlloc(origin, radius, direction, entries, length);
            hitNumber2 = Physics.SphereCastNonAlloc(origin + (direction * length), radius, -direction, exits, length);
        }

        for (int i = 0; i < Mathf.Min(hitNumber1, entries.Length); i++)
        {
            hits.Add(entries[i]);
        }

        for (int i = 0; i < Mathf.Min(hitNumber2, exits.Length); i++)
        {
            exits[i].distance = length - exits[i].distance;
            //exits[i].distance -= Vector3.Distance(exits[i].point, entries[i].point);
            hits.Add(exits[i]);
        }

        //hits.Sort((x, y) => x.distance.CompareTo(y.distance));
    }

    public void DoSoundDirectionality(RayTracedAudioSource source)
    {
        Vector3 toSource = (source.m_virtualPosition - transform.position).normalized;

        // Project both vectors onto the horizontal plane (Y axis ignored)
        Vector3 listenerForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        Vector3 toSourceFlat = Vector3.ProjectOnPlane(toSource, Vector3.up).normalized;

        // Get the signed angle between the listener's forward and the direction to the sound
        float angle = Vector3.SignedAngle(listenerForward, toSourceFlat, Vector3.up);
        print(angle);

        float pan = Mathf.Sin(angle * Mathf.Deg2Rad);
        pan = Mathf.Clamp(pan, -0.8f, 0.8f);
        source.SetPanning(pan);

        
    }
}