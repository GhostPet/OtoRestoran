using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class MissionManager : MonoBehaviour
{
    public GameObject missionButtonPrefab;
    public Transform missionContainer;

    private List<MissionData> missions = new List<MissionData>();

    public void AddMission(string name, string code)
    {
        MissionData data = new MissionData();
        data.missionName = name;
        data.missionCode = code;

        missions.Add(data);

        GameObject newButton =
            Instantiate(missionButtonPrefab, missionContainer);

        newButton.GetComponentInChildren<TMP_Text>().text = name;

        newButton.GetComponent<Button>().onClick.AddListener(() =>
        {
            Debug.Log("Çalıştırılan Mission: " + name);
        });
    }
}