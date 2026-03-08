using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class ProgramsPanelManager : MonoBehaviour
{
    public GameObject robotPanelPrefab;
    public GameObject programBoxPrefab;
    public Transform contentRoot;

    Dictionary<int, Transform> robotPanels = new Dictionary<int, Transform>();

    public void AddProgram(int robotID, string programName)
    {
        Transform robotPanel;

        if (!robotPanels.ContainsKey(robotID))
        {
            GameObject panel = Instantiate(robotPanelPrefab, contentRoot);
            robotPanel = panel.transform;

            robotPanels.Add(robotID, robotPanel);
        }
        else
        {
            robotPanel = robotPanels[robotID];
        }

        Transform content = robotPanel.Find("Scroll View/Viewport/Content");

        GameObject box = Instantiate(programBoxPrefab, content);

        box.GetComponentInChildren<Text>().text = programName;
    }
}