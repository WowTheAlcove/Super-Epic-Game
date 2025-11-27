using UnityEngine;

public class QuestIcon : MonoBehaviour
{
    [Header("Icons")]
    [SerializeField] private GameObject requirementsNotMetToStartIcon;
    [SerializeField] private GameObject canStartIcon;
    [SerializeField] private GameObject requirementsNotMetToFinishIcon;
    [SerializeField] private GameObject canFinishIcon;

    public void SetState(QuestState newState, bool isStartPoint, bool isFinishPoint) {
        requirementsNotMetToStartIcon.SetActive(false);
        canStartIcon.SetActive(false);
        requirementsNotMetToFinishIcon.SetActive(false);
        canFinishIcon.SetActive(false);

        switch (newState) {
            case QuestState.REQUIREMENTS_NOT_MET:
                if (isStartPoint) {
                    requirementsNotMetToStartIcon.SetActive(true);
                }
                break;
            case QuestState.CAN_START:
                if (isStartPoint) {
                    canStartIcon.SetActive(true);
                }
                break;
            case QuestState.IN_PROGRESS:
                if (isFinishPoint) {
                    requirementsNotMetToFinishIcon.SetActive(true);
                }
                break;
            case QuestState.CAN_FINISH:
                if (isFinishPoint) {
                    canFinishIcon.SetActive(true);
                }
                break;
            case QuestState.FINISHED:
                break;
            default:
                Debug.LogError("Quest State not recognized by switch statement for quest icon: " + newState);
                break;
        }

    }
}
