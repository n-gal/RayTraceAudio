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
    private List<RayTracedAudioSource> m_sources = new List<RayTracedAudioSource>();
    private List<Vector3> m_averageSoundPositions = new List<Vector3>();

    void Awake()
    {
        m_sources = new List<RayTracedAudioSource>(Resources.FindObjectsOfTypeAll<RayTracedAudioSource>());
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        foreach (Vector3 position in m_averageSoundPositions)
        {
            Gizmos.DrawWireSphere(position, 3f);
        }
    }

    void Update()
    {
        foreach (RayTracedAudioSource source in m_sources)
            source.ClearHearPoints();
        List<Vector3> averageSoundPositions = new List<Vector3>();

        //DoSoundFalloff();
        CreateBouncingRay(transform.position, transform.forward, 8);
        CreateSensingRays(transform.position, 100, 8);

        foreach (RayTracedAudioSource source in m_sources)
        {

            List<Vector3> heardPositions = new List<Vector3>();
            heardPositions = source.m_heardPositions;
            Vector3 newSoundPos = AverageVec3(heardPositions);
            averageSoundPositions.Add(newSoundPos);
        }
        m_averageSoundPositions = averageSoundPositions;

    }

    void DoSoundFalloff()
    {
        foreach (RayTracedAudioSource source in m_sources)
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

    private bool IsSoundVisible(RayTracedAudioSource source)
    {
        Vector3 direction = (source.transform.position - transform.position).normalized;
        RaycastHit hit;
        return !Physics.Raycast(transform.position, direction, out hit);
    }

    void CreateSensingRays(Vector3 startPosition, int numRays, int bounces)
    {
        float phi = Mathf.PI * (3f - Mathf.Sqrt(5f)); 

        for (int i = 0; i < numRays; i++)
        {
            float y = 1f - (i / (float)(numRays - 1)) * 2f; 
            float radius = Mathf.Sqrt(1 - y * y); 

            float theta = phi * i;

            float x = Mathf.Cos(theta) * radius;
            float z = Mathf.Sin(theta) * radius;

            Vector3 dir = new Vector3(x, y, z);

            CreateBouncingRay(startPosition, dir, bounces);
        }
    }

    private void CreateBouncingRay(Vector3 startPosition, Vector3 startDirection, int bounces)
    {
        if(bounces >0)
        {
            RaycastHit hit;
            if (Physics.Raycast(startPosition, startDirection, out hit, 1000))
            {
                bounces--;

                
                RaycastHit parentHit = hit;
                foreach (RayTracedAudioSource source in m_sources)
                {
                    // pull the ray slightly away from the hit point so as to prevent it from passing through walls
                    Vector3 adjustedHitPos = parentHit.point + (0.1f * parentHit.normal);
                    Vector3 direction = (source.transform.position - adjustedHitPos).normalized;
                    float distance = Vector3.Distance(adjustedHitPos, source.transform.position);
                    if (!Physics.Raycast(adjustedHitPos, direction, out hit, distance))
                    {
                        Debug.DrawRay(startPosition, startDirection * parentHit.distance, Color.green);
                        source.m_heardPositions.Add(parentHit.point);
                        Debug.DrawRay(adjustedHitPos, direction * distance, Color.blue);
                    }
                }
                Vector3 reflectDir = Vector3.Reflect(startDirection, parentHit.normal);
                CreateBouncingRay(parentHit.point, reflectDir, bounces);
            }
            else
            {
                //Debug.DrawRay(startPosition, startDirection * 10000, Color.red);
            }
        }
    }

    private Vector3 AverageVec3(List<Vector3> vector3s)
    {
        Vector3 average = Vector3.zero;
        foreach (Vector3 v in vector3s)
            average += v;

        average = average / vector3s.Count;
        return average;
    }
}