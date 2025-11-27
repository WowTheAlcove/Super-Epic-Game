// Assets/Editor/NetworkManagerAutoShutdown.cs
using UnityEditor;
using UnityEngine;
using Unity.Netcode;

[InitializeOnLoad]
public static class NetworkManagerAutoShutdown {
    static NetworkManagerAutoShutdown() {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state) {
        if (state == PlayModeStateChange.ExitingPlayMode) {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening) {
                Debug.Log("Auto-shutting down NetworkManager before exiting Play Mode.");
                NetworkManager.Singleton.Shutdown();
                EditorApplication.playModeStateChanged -= OnPlayModeChanged; // Unsubscribe to prevent multiple calls
            }
        }
    }
}
