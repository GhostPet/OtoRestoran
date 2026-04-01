using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class CarMovement : MonoBehaviour
{
    private NavMeshAgent agent;
    private Transform customTarget;
    private bool isDestroying = false;

    void Awake()
    {
        // Komponenti alalım
        agent = GetComponent<NavMeshAgent>();
    }

    // TrafficManager tarafından çağrılacak fonksiyon
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
        // Eğer hedefimiz varsa ve henüz yok olma sürecinde değilsek
        if (customTarget != null && !isDestroying)
        {
            // Hedefe olan mesafeyi ölç (1.5 birimden azsa yok olmaya başla)
            float distance = Vector3.Distance(transform.position, customTarget.position);
            
            if (distance < 1.5f)
            {
                StartCoroutine(ShrinkAndDestroy());
            }
        }
    }

    IEnumerator ShrinkAndDestroy()
    {
        isDestroying = true;
        
        // Arabayı durdur
        if (agent != null) 
        {
            agent.isStopped = true;
            agent.enabled = false; // Diğer araçlarla çakışmaması için navigasyonu kapat
        }

        float timer = 0;
        float duration = 0.8f; // Yok olma hızı (saniye)
        Vector3 originalScale = transform.localScale;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            // Boyutu sıfıra indir
            transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, timer / duration);
            yield return null;
        }

        // Sahneden tamamen sil
        Destroy(gameObject);
    }
}