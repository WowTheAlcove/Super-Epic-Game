using Unity.Netcode;
using UnityEngine;

public class GameEventsManager : NetworkBehaviour
{
    public static GameEventsManager Instance {get; private set; }

    public MiscEvents miscEvents;
    public QuestEvents questEvents;

    private void Awake() {
        if (Instance != null) {
            Debug.LogError("More than one GameEventsManager tried to be instantiated");
        }
        
        Instance = this;

        questEvents = new QuestEvents();
        miscEvents = new MiscEvents();
    }
}
