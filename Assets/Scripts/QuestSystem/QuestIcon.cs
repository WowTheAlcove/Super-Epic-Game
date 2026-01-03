using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;

public class QuestIcon : MonoBehaviour
{
    [Serializable]
    struct QuestToDisplay
    {
        public QuestInfoSO questInfoSo;
        public bool isStartPoint;
        public bool isFinishPoint;
    }
    
    [Header("Tracked Quests")]
    [SerializeField] private List<QuestToDisplay> trackedQuests;
    
    [Header("Icons")]
    [SerializeField] private GameObject cannotYetStartIcon;
    [SerializeField] private GameObject canStartIcon;
    [SerializeField] private GameObject cannotYetFinishIcon;
    [SerializeField] private GameObject canFinishIcon;

    private Dictionary<string, QuestState> trackedQuestStates;
    private QuestState furthestState;
    private QuestToDisplay furthestQuestToDisplay;

    #region ==== INITIALIZATION ==== 
    
    private void Awake()
    {
        ClearIcon();
        
        if (trackedQuests.Count == 0)
        {
            return;
        }
        
        trackedQuestStates = new Dictionary<string, QuestState>();
    }
    
    //On Start, request the quest manager to report the state of all the quests this tracks
    private void Start()
    {
        foreach (QuestToDisplay quest in trackedQuests)
        {
            trackedQuestStates.Add(quest.questInfoSo.Id, QuestState.NONE);
            GameEventsManager.Instance.questEvents.InvokeRequestQuestState(this, quest.questInfoSo.Id, (int)(NetworkManager.Singleton.LocalClientId));
        }
        GameEventsManager.Instance.questEvents.OnQuestStateChange += QuestEvents_OnQuestStateChange;
    }

    private void OnDisable()
    {
        GameEventsManager.Instance.questEvents.OnQuestStateChange -= QuestEvents_OnQuestStateChange;
    }
    #endregion

    //Pre: A quest has either had its state changed or requested
    //Checks to see if the quest's state is tracked
    //If it is, then it checks if it's quest state is 
    private void QuestEvents_OnQuestStateChange(object sender, QuestEvents.QuestStateChangeEventArgs e)
    {
        if (trackedQuests.Count == 0)
        {
            return;
        }
        if(!trackedQuestStates.ContainsKey(e.QuestID)) //If the quest id was not tracked, don't do anything
        {
            return;
        }

        trackedQuestStates[e.QuestID] = e.NewQuestState;
        SetFurthestStateAndQuestToDisplay();
        SetVisualOfFurthestState();
    }

    //Set the furthestState to that which isn't finished
    //And track the QuestToDisplay of that state's quest, so that it can be used for visual
    private void SetFurthestStateAndQuestToDisplay()
    {
        furthestState = QuestState.NONE;
        foreach(KeyValuePair<string, QuestState> kvp in trackedQuestStates)
        {
            if(kvp.Value >  furthestState && kvp.Value != QuestState.FINISHED)
            {
                furthestState = kvp.Value;
                furthestQuestToDisplay = trackedQuests.Find(questToDisplay => questToDisplay.questInfoSo.Id == kvp.Key);
            }
        }
    }
    
    public void SetVisualOfFurthestState() {
        ClearIcon();

        switch (furthestState) {
            case QuestState.REQUIREMENTS_NOT_MET:
                if (furthestQuestToDisplay.isStartPoint) {
                    cannotYetStartIcon.SetActive(true);
                }
                break;
            case QuestState.CAN_START:
                if (furthestQuestToDisplay.isStartPoint) {
                    canStartIcon.SetActive(true);
                }
                break;
            case QuestState.IN_PROGRESS:
                if (furthestQuestToDisplay.isFinishPoint) {
                    cannotYetFinishIcon.SetActive(true);
                }
                break;
            case QuestState.CAN_FINISH:
                if (furthestQuestToDisplay.isFinishPoint) {
                    canFinishIcon.SetActive(true);
                }
                break;
            case QuestState.FINISHED:
                break;
            case QuestState.NONE:
                break;
            default:
                Debug.LogError("Quest State not recognized by switch statement for quest icon: " + furthestState.ToString());
                break;
        }

    }

    private void ClearIcon()
    {
        cannotYetStartIcon.SetActive(false);
        canStartIcon.SetActive(false);
        cannotYetFinishIcon.SetActive(false);
        canFinishIcon.SetActive(false);
    }
}