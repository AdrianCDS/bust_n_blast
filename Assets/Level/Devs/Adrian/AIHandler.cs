using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.AI;

public class AIHandler : NetworkBehaviour
{
    public Transform target;
    [Networked] public Vector3 TargetPoint { get; set; }

    private NavMeshPath navMeshPath;
    bool isPathComplete = false;

    [Networked] public bool IsMovingToTarget { get; set; }
    [Networked] public bool NextPointIsShield { get; set; }
    [Networked] public bool ReachedShield { get; set; }

    Vector3 lastRecreatePathPosition;

    [SerializeField] private float pathFindingRadius = 30f;

    void OnEnable()
    {
        Events.OnProtectionShieldPlaced += HandleProtectionShieldPlaced;
        Events.OnProtectionShieldRemoved += HandleProtectionShieldRemoved;
    }

    void OnDisable()
    {
        Events.OnProtectionShieldPlaced -= HandleProtectionShieldPlaced;
        Events.OnProtectionShieldRemoved -= HandleProtectionShieldRemoved;
    }

    void Start()
    {
        target = null;
        navMeshPath = new NavMeshPath();
    }

    private Transform SetPotentialTarget()
    {
        GameObject[] potentialTargets = ExcludeTargetFromArray(GameObject.FindGameObjectsWithTag("ProtectionShield"), target);

        if (potentialTargets.Length == 0)
        {
            return null;
        }
        else
        {
            Transform closestTarget = null;
            float closestDistanceSqr = Mathf.Infinity;
            Vector3 currentPosition = transform.position;

            foreach (GameObject potentialTarget in potentialTargets)
            {
                Vector3 directionToTarget = potentialTarget.transform.position - currentPosition;
                float dSqrToTarget = directionToTarget.sqrMagnitude;

                if (dSqrToTarget < closestDistanceSqr)
                {
                    closestDistanceSqr = dSqrToTarget;
                    closestTarget = potentialTarget.transform;
                }
            }

            return closestTarget;
        }
    }

    public bool TryGetReachablePoint(Vector3 origin, float radius, int attempts, out Vector3 validPoint)
    {
        if (NextPointIsShield && target == null)
        {
            target = SetPotentialTarget();
        }

        if (target != null && (Vector3.Distance(origin, target.transform.position) <= radius))
        {
            validPoint = target.transform.position;
            return true;
        }

        NextPointIsShield = false;

        for (int i = 0; i < attempts; i++)
        {
            Vector3 randomPoint = RandomNavSphere(origin, radius, NavMesh.AllAreas);

            if (NavMesh.CalculatePath(origin, randomPoint, NavMesh.AllAreas, navMeshPath) && navMeshPath.status == NavMeshPathStatus.PathComplete)
            {
                validPoint = randomPoint;
                return true;
            }
        }

        validPoint = Vector3.zero;
        return false;
    }

    public static Vector3 RandomNavSphere(Vector3 origin, float dist, int layermask)
    {
        Vector3 randDirection = Random.insideUnitSphere * dist;

        randDirection += origin;

        NavMesh.SamplePosition(randDirection, out NavMeshHit navHit, dist, layermask);

        return navHit.position;
    }

    public Vector3 GetDirectionToTarget(bool isAlive, out float distanceToTarget)
    {
        distanceToTarget = 0f;

        if (target != null && ReachedShield || !isAlive) return Vector3.zero;

        if (!IsMovingToTarget && TryGetReachablePoint(transform.position, pathFindingRadius, 10, out Vector3 point))
        {
            TargetPoint = point;
            IsMovingToTarget = true;
        }

        distanceToTarget = (TargetPoint - transform.position).magnitude;

        if ((transform.position - lastRecreatePathPosition).magnitude < 1)
        {
            return GetVectorToPath();
        }

        isPathComplete = NavMesh.CalculatePath(transform.position, TargetPoint, NavMesh.AllAreas, navMeshPath);

        return GetVectorToPath();
    }

    private Vector3 GetVectorToPath()
    {
        Vector3 vectorToPath;

        if (navMeshPath.corners.Length > 1)
        {
            vectorToPath = navMeshPath.corners[1] - transform.position;
        }
        else
        {
            vectorToPath = TargetPoint - transform.position;
        }

        vectorToPath.Normalize();

        return vectorToPath;
    }

    private void HandleProtectionShieldPlaced()
    {
        NextPointIsShield = true;
    }

    private void HandleProtectionShieldRemoved(Transform shieldTransform)
    {
        if (target != null && ReachedShield && shieldTransform.position == target.position)
        {
            Transform newTarget = SetPotentialTarget();
            if (newTarget == null)
            {
                ReachedShield = false;
                NextPointIsShield = false;
                target = null;
            }
            else
            {
                ReachedShield = false;
                NextPointIsShield = true;
                target = newTarget;
            }
        }
    }

    public GameObject[] ExcludeTargetFromArray(GameObject[] originalArray, Transform toRemoveTransform)
    {
        List<GameObject> result = new List<GameObject>();

        foreach (var obj in originalArray)
        {
            if (obj != null && obj.transform != toRemoveTransform)
            {
                result.Add(obj);
            }
        }

        return result.ToArray();
    }

    public void Stop()
    {
        target = null;
        ReachedShield = false;
        NextPointIsShield = false;
    }
}
