using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerIndexSelectionUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject selectionPanel;
    [SerializeField] private Button[] indexButtons; // Buttons for player indices
    [SerializeField] private TextMeshProUGUI statusText;
    
    #region ==== INITIALIZATION/CLEANUP ====
    private void Start()
    {
        selectionPanel.SetActive(false);
        
        for (int i = 0; i < indexButtons.Length; i++)
        {
            int index = i + 1; // Buttons represent indices 1, 2, 3
            indexButtons[i].onClick.AddListener(() => OnIndexButtonClicked(index));
        }
        
        
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
        
        if (PlayerIndexManager.Instance != null)
        {
            PlayerIndexManager.Instance.OnPlayerIndexAssigned += OnAnyPlayerIndexAssigned;
        }
    }
    
    private void OnEnable()
    {
    }
    
    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
        
        if (PlayerIndexManager.Instance != null)
        {
            PlayerIndexManager.Instance.OnPlayerIndexAssigned -= OnAnyPlayerIndexAssigned;
        }
    }
    #endregion
    
    //When a client connects, show the UI if it's the local player and isn't the host
    private void OnClientConnected(ulong clientId)
    {
        // Only show UI for local non-host clients
        if (clientId == NetworkManager.Singleton.LocalClientId && !NetworkManager.Singleton.IsHost)
        {
            ShowSelectionUI();
        }
    }
    
    private void ShowSelectionUI()
    {
        selectionPanel.SetActive(true);
        statusText.text = "Choose your player slot (1-3)";
        UpdateButtonStates();
    }
    
    // Makes all buttons for available PlayerIndices clickable, and vice versa
    private void UpdateButtonStates()
    {
        if (PlayerIndexManager.Instance == null) return;
        
        List<int> availableIndices = PlayerIndexManager.Instance.GetAvailableIndices();
        
        for (int i = 0; i < indexButtons.Length; i++)
        {
            int index = i + 1;
            indexButtons[i].interactable = availableIndices.Contains(index);
            
            // Optional: Update button text to show availability
            TextMeshProUGUI buttonText = indexButtons[i].GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = availableIndices.Contains(index) 
                    ? $"Player {index}" 
                    : $"Player {index} (Taken)";
            }
        }
    }
    
    // Pre: A button to select a Player Index was clicked
    // Request a PlayerIndex on the PlayerIndexManager
    // Disables buttons until further notice
    private void OnIndexButtonClicked(int selectedIndex)
    {
        if (PlayerIndexManager.Instance == null)
        {
            Debug.LogError("PlayerIndexManager not found");
            return;
        }
        
        statusText.text = $"Requesting Player {selectedIndex}...";
        
        // Disable all buttons while processing
        foreach (var button in indexButtons)
        {
            button.interactable = false;
        }
        
        PlayerIndexManager.Instance.RequestPlayerIndex(selectedIndex);
    }
    
    // Pre: A PlayerIndex is assigned
    // Update which buttons should be clickable, close the UI if we were the ones who chose a player index
    private void OnAnyPlayerIndexAssigned(int playerIndex, ulong clientId)
    {
        // Update button states when any player claims an index
        UpdateButtonStates();
        
        // If this is our local client, close UI
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            statusText.text = $"You are Player {playerIndex}!";
            CloseUI();
        }
    }
    
    private void CloseUI()
    {
        selectionPanel.SetActive(false);
    }
}