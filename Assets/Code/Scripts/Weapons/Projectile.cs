using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Projectile Setup")]
    public float Speed = 80f;
    public float MaxDistance = 100f;
    public float LifeTimeAfterHit = 2f;
    public float damage = 10f;

    [Header("Impact Setup")]
    public GameObject ProjectileObject;
    public GameObject HitEffectPrefab;

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private Vector3 hitNormal;

    private bool showHitEffect;

    private float startTime;
    private float duration;

    public void SetHit(Vector3 _hitPosition, Vector3 _hitNormal, bool _showHitEffect)
    {
        targetPosition = _hitPosition;
        showHitEffect = _showHitEffect;
        hitNormal = _hitNormal;
    }

    private void Start()
    {
        startPosition = transform.position;

        if (targetPosition == Vector3.zero)
        {
            targetPosition = startPosition + transform.forward * MaxDistance;
        }

        duration = Vector3.Distance(startPosition, targetPosition) / Speed;
        startTime = Time.timeSinceLevelLoad;
    }

    private void Update()
    {
        float time = Time.timeSinceLevelLoad - startTime;

        if (time < duration)
        {
            transform.position = Vector3.Lerp(startPosition, targetPosition, time / duration);
        }
        else
        {
            transform.position = targetPosition;
            FinishProjectile();
        }
    }

    private void FinishProjectile()
    {
        if (showHitEffect == false)
        {
            // No hit effect, destroy immediately.
            Destroy(gameObject);
            return;
        }

        // Stop updating projectile visual.
        enabled = false;

        if (ProjectileObject != null)
        {
            ProjectileObject.SetActive(false);
        }

        if (HitEffectPrefab != null)
        {
            Instantiate(HitEffectPrefab, targetPosition, Quaternion.LookRotation(hitNormal), transform);
        }

        Destroy(gameObject, LifeTimeAfterHit);
    }
}
