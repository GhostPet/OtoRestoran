using UnityEngine;

public class RobotSaveButton : MonoBehaviour
{
    public int robotID;

    public void PressSave()
    {
        SaveProgramPopup popup = FindObjectOfType<SaveProgramPopup>();

        popup.OpenPopup(robotID);
    }
}