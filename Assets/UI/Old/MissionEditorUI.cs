using UnityEngine;
using TMPro;

public class MissionEditorUI : MonoBehaviour
{
    public GameObject editorPanel;

    public TMP_InputField missionNameInput;
    public TMP_InputField codeInput;

    public MissionManager missionManager;

    void Start()
    {
        editorPanel.SetActive(false);
    }

    public void OpenEditor()
    {
        editorPanel.SetActive(true);
    }

    public void CloseEditor()
    {
        editorPanel.SetActive(false);
    }

    public void SaveMission()
    {
        string name = missionNameInput.text;
        string code = codeInput.text;

        if (string.IsNullOrEmpty(name))
        {
            Debug.Log("Mission ismi boş!");
            return;
        }

        missionManager.AddMission(name, code);

        editorPanel.SetActive(false);

        missionNameInput.text = "";
        codeInput.text = "";
    }
}