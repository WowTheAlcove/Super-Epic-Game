using Mono.Cecil.Cil;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(CircleCollider2D))]
public class QuestPoint : MonoBehaviour
{
    [Header("Quest")]
    [SerializeField] private QuestInfoSO questInfoForPoint;

    [Header("Config")]
    [SerializeField] private bool isStartPoint = true;
    [SerializeField] private bool isFinishPoint = true;

    private bool playerIsNear = false;
    PlayerInputActions inputActions;

    private string questId;
    private QuestState currentQuestState;

    private QuestIcon questIcon;

    private void Awake() {
        questId = questInfoForPoint.id;
        questIcon = GetComponentInChildren<QuestIcon>();
    }

    private void OnEnable() {
        GameEventsManager.Instance.questEvents.OnQuestStateChange += QuestEvents_OnQuestStateChange;
        inputActions = new PlayerInputActions();
        inputActions.Player.Enable();
        inputActions.Player.BingoBongo.performed += BingoBongo_performed;
    }

    private void OnDisable() {
        GameEventsManager.Instance.questEvents.OnQuestStateChange -= QuestEvents_OnQuestStateChange;
        inputActions.Dispose();
    }

    private void BingoBongo_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj) {
        if (!playerIsNear) {
            return;
        }
        
        if (currentQuestState == QuestState.CAN_START && isStartPoint) {
            GameEventsManager.Instance.questEvents.InvokeStartQuest(this, questId, (int)NetworkManager.Singleton.LocalClientId);
        } else if (currentQuestState == QuestState.CAN_FINISH && isFinishPoint){
            GameEventsManager.Instance.questEvents.InvokeFinishQuest(this, questId, (int)NetworkManager.Singleton.LocalClientId);
        }
    }

    private void QuestEvents_OnQuestStateChange(object sender, QuestEvents.QuestStateChangeEventArgs e) {
        if (e.QuestID == questInfoForPoint.id) {
            currentQuestState = e.NewQuestState;

            questIcon.SetState(currentQuestState, isStartPoint, isFinishPoint);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision) {
        PlayerController enteringPlayer = collision.GetComponent<PlayerController>();
        if (enteringPlayer != null && enteringPlayer.IsOwner) {
            playerIsNear = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision) {
        PlayerController enteringPlayer = collision.GetComponent<PlayerController>();
        if (enteringPlayer != null && enteringPlayer.IsOwner) {
            playerIsNear = false;
        }
    }
}
