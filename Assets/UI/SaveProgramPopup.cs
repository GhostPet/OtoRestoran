using UnityEngine;
using UnityEngine.UI;

public class SaveProgramPopup : MonoBehaviour
{
    public InputField nameInput;

    public ProgramsPanelManager panelManager;

    int robotID;

    public void OpenPopup(int id)
    {
        robotID = id;
        gameObject.SetActive(true);
    }

    public void SaveProgram()
    {
        string programName = nameInput.text;

        panelManager.AddProgram(robotID, programName);

        nameInput.text = "";

        gameObject.SetActive(false);
    }

    public void Cancel()
    {
        gameObject.SetActive(false);
    }
}