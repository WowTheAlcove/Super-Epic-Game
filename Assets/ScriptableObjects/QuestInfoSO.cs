using UnityEngine;

[CreateAssetMenu(fileName = "QuestInfoSO", menuName = "Scriptable Objects/QuestInfoSO", order = 1)]
public class QuestInfoSO : ScriptableObject
{
    [field: SerializeField] public string Id { get; private set; }

    [SerializeField] public bool IsShared;

    [Header("General")]
    public string displayName;

    [Header("Requirements")]
    public int bingoBongoRequirement;
    public QuestInfoSO[] prerequisiteQuestInfoSos;

    [Header("Steps")]
    public GameObject[] questStepPrefabs;

    [Header("Rewards")]
    public GameObject questRewardPrefab;
}