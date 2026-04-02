using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class CarMovement : MonoBehaviour
{
    private NavMeshAgent agent;
    private Transform customTarget;
    private bool isDestroying = false;
    private ParticleSystem exhaustSmoke;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        // Araba içindeki duman efektini otomatik bul
        exhaustSmoke = GetComponentInChildren<ParticleSystem>();
    }

    public void SetTarget(Transform target)
    {
        customTarget = target;
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        
        if (agent != null && customTarget != null)
        {
            agent.SetDestination(customTarget.position);
        }
    }

    void Update()
    {
        if (customTarget != null && !isDestroying)
        {
            // Hedefe yaklaşıp yaklaşmadığını kontrol et
            float distance = Vector3.Distance(transform.position, customTarget.position);
            
            // 1.5 birim mesafe kalınca yok olma sürecini başlat
            if (distance < 1.5f)
            {
                StartCoroutine(ShrinkAndDestroy());
            }
        }
    }

    IEnumerator ShrinkAndDestroy()
    {
        isDestroying = true;

        // 1. Dumanı durdur (havadakiler kalır, yeni duman çıkmaz)
        if (exhaustSmoke != null)
        {
            exhaustSmoke.Stop();
        }
        
        // 2. Arabayı durdur ve navigasyonu kapat
        if (agent != null) 
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        // 3. Küçülme animasyonu
        float timer = 0;
        float duration = 0.8f; 
        Vector3 originalScale = transform.localScale;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, timer / duration);
            yield return null;
        }

        // 4. Objeyi tamamen yok et
        Destroy(gameObject);
    }
}