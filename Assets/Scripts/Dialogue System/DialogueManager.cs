using UnityEngine.InputSystem;
using UnityEngine;
using Ink.Runtime;
using Unity.Netcode;

public class DialogueManager : MonoBehaviour
{
    [Header("Ink Story")]
    [SerializeField] private TextAsset inkJson;

    private Story story;
    private bool dialoguePlaying = false;
    private PlayerInputActions myInputActions;
    private PlayerController localPlayer;

    private void Awake()
    {
        story = new Story(inkJson.text);
        myInputActions = new PlayerInputActions();
    }

    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
    }
    private void OnEnable()
    {
        GameEventsManager.Instance.dialogueEvents.OnEnterDialogue += DialogueEvents_OnEnterDialogue;
        myInputActions.Dialogue.SkipDialogue.performed += SkipDialogue_performed;
    }


    private void OnDisable()
    {
        GameEventsManager.Instance.dialogueEvents.OnEnterDialogue -= DialogueEvents_OnEnterDialogue;    
        myInputActions.Dialogue.SkipDialogue.performed -= SkipDialogue_performed;
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= NetworkManager_OnClientConnectedCallback;
        }
    }

    private void DialogueEvents_OnEnterDialogue(object sender, DialogueEvents.DialogueEventArgs e)
    {
        if(dialoguePlaying) {
            return;
        }

        dialoguePlaying = true;
        myInputActions.Dialogue.Enable();
        localPlayer.DisableInput();

        if (!e.KnotName.Equals("")) {
            story.ChoosePathString(e.KnotName);
        } else {
            Debug.LogError("Tried to enter dialogue at knot name that doesn't exist: " + e.KnotName);
        }

        ContinueOrExitStory();
    }

    private void ContinueOrExitStory()
    {
        if (story.canContinue)
        {
            string dialogueLine = story.Continue();
            Debug.Log(dialogueLine);
        } else
        {
            ExitDialogue();
        }
    }

    private void ExitDialogue()
    {
        Debug.Log("Exiting Dialogue");
        dialoguePlaying = false;
        myInputActions.Dialogue.Disable();
        localPlayer.EnableInput();
        story.ResetState();
    }
    private void SkipDialogue_performed(InputAction.CallbackContext obj)
    {
        if(dialoguePlaying)
        {
            ContinueOrExitStory();
        }
    }
    private void NetworkManager_OnClientConnectedCallback(ulong obj)
    {
        localPlayer = FindLocalPlayer();
    }

    private PlayerController FindLocalPlayer()
    {
        foreach (PlayerController player in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
        {
            if (player.IsLocalPlayer)
            {
                return player;
            }
        }
        Debug.LogError("Could not find local player in DialogueManager");
        return null;
    }

}
