using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using Unity.Hierarchy;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.UI.Image;

public class RayTraceTest : MonoBehaviour
{
    [SerializeField] private AnimationCurve m_soundFalloff;
    [SerializeField] private AnimationCurve m_soundFalloffDampening;
    private List<AudioSource> m_sources = new List<AudioSource>();

    void Awake()
    {
        m_sources = new List<AudioSource>(Resources.FindObjectsOfTypeAll<AudioSource>());
    }


    void Update()
    {
        //DoSoundFalloff();
        CreateBouncingRay(transform.position, transform.forward, 8);
    }

    void DoSoundFalloff()
    {
        foreach (AudioSource source in m_sources)
        {
            float distance = Vector3.Distance(source.transform.position, transform.position);
            CheckSoundObstruction(source.transform, distance);
        }
    }

    void CheckSoundObstruction(Transform targetAudioSource, float audioSourceDistance)
    {
        Vector3 direction = (targetAudioSource.position - transform.position).normalized;

        RaycastHit[]    entries = new RaycastHit[8],
                        exits = new RaycastHit[8];

        List<RaycastHit> intersections = new List<RaycastHit>(16);

        // Does the ray intersect any objects excluding the player layer
        float radius = 0f;
        float distance = audioSourceDistance;
        float distanceThroughWall = 0;
        Vector3 startRayTarget = transform.position;

        BidirectionalRaycastNonAlloc(transform.position, radius, direction, distance, ref entries, ref exits, ref intersections);

        //Debug.DrawLine(transform.position, targetAudioSource.position);

        for (int i = 0; i < exits.Length - 1 ; i++)
        {
            if (exits[i].normal != Vector3.zero)
            {
                distanceThroughWall += Vector3.Distance(entries[i].point, exits[i].point);

                Debug.DrawLine(intersections[i].point, intersections[i].point + (intersections[i].normal * 0.25f), Color.green);

                Debug.DrawLine(entries[i].point, exits[i].point, Color.red);
            }
        }
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

    private void CreateBouncingRay(Vector3 startPosition, Vector3 startDirection, float bounces)
    {
        if(bounces >0)
        {
            RaycastHit hit;
            if (Physics.Raycast(startPosition, startDirection, out hit, 1000))
            {
                bounces--;
                Vector3 reflectDir = Vector3.Reflect(startDirection, hit.normal);
                Debug.DrawRay(startPosition, startDirection * hit.distance, Color.green);
                CreateBouncingRay(hit.point, reflectDir, bounces);
            }
            else
            {
                Debug.DrawRay(startPosition, startDirection * 10000, Color.red);
            }
        }
    }
}