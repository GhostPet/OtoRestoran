using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TrafficManager : MonoBehaviour
{
    [System.Serializable]
    public class LaneConfig // Her şerit için özel ayar
    {
        public Transform spawnPoint;
        public Transform targetPoint;
        public GameObject[] carPrefabs;
    }

    public List<LaneConfig> lanes; // Inspector'dan kaç şerit istersen ekle
    public float minSpawnTime = 2f;
    public float maxSpawnTime = 5f;

    void Start()
    {
        foreach (var lane in lanes)
        {
            StartCoroutine(SpawnRoutine(lane));
        }
    }

    IEnumerator SpawnRoutine(LaneConfig lane)
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minSpawnTime, maxSpawnTime));
            
            int randomIndex = Random.Range(0, lane.carPrefabs.Length);
            GameObject car = Instantiate(lane.carPrefabs[randomIndex], lane.spawnPoint.position, lane.spawnPoint.rotation);
            
            // Araca hangi hedefe gitmesi gerektiğini söyleyelim
            CarMovement moveScript = car.GetComponent<CarMovement>();
            if (moveScript != null)
            {
                moveScript.SetTarget(lane.targetPoint);
            }
        }
    }
}