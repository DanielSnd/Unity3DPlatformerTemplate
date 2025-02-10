using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages checkpoint state and persistence across scene loads.
/// Stores checkpoint positions and handles player respawning.
/// </summary>
public static class CheckpointManager
{
    public static List<Checkpoint> checkpoints = new List<Checkpoint>();
    /// <summary>
    /// Current checkpoint data stored as Vector4 where:
    /// x,y,z = position
    /// w = scene hash
    /// </summary>
    private static Vector4 currentCheckpoint = Vector4.zero;
    
    /// <summary>
    /// Cache of scene name hashes to avoid recalculation
    /// </summary>
    private static readonly Dictionary<string, int> sceneHashCache = new Dictionary<string, int>();
    
    public static void ClearSceneHashCache() {sceneHashCache.Clear();}
    /// <summary>
    /// The current active checkpoint position and scene hash
    /// </summary>
    public static Vector4 CurrentCheckpoint => currentCheckpoint;
    
    /// <summary>
    /// Sets a new checkpoint position and associates it with the current scene
    /// </summary>
    /// <param name="position">World position of the checkpoint</param>
    /// <param name="sceneName">Name of the scene containing the checkpoint</param>
    public static void SetCheckpoint(Vector3 position, string sceneName)
    {
        SetCheckpoint(position, GetSceneHash(sceneName));
    }

    /// <summary>
    /// Sets a new checkpoint position and associates it with the current scene
    /// </summary>
    /// <param name="position">World position of the checkpoint</param>
    /// <param name="sceneNameHash">Hash of the name of the scene containing the checkpoint</param>
    public static void SetCheckpoint(Vector3 position, int sceneNameHash)
    {
        currentCheckpoint = new Vector4(
            position.x,
            position.y,
            position.z,
            sceneNameHash
        );
    }

    /// <summary>
    /// Handles scene loading by checking if the checkpoint belongs to this scene
    /// and teleporting the player if it does
    /// </summary>
    public static void HandleSceneLoad(string sceneName)
    {
        int sceneNameHash = GetSceneHash(sceneName);
        if (currentCheckpoint.w != sceneNameHash)
        {
            // We're in a different scene, we'll attempt to find a checkpoint and set it (one marked initial if possible)
            foreach (var cpoint in checkpoints)
            {
                SetCheckpoint(cpoint.transform.position, sceneNameHash);
                if (cpoint.IsInitialCheckpoint)
                    break;
            }
        }
        if (currentCheckpoint.w == sceneNameHash)
        {
            TeleportPlayerToCheckpoint();
        }
    }
    
    /// <summary>
    /// Teleports a specific player or all players to the current checkpoint position
    /// </summary>
    public static void TeleportPlayerToCheckpoint(GameObject playerToTeleport = null)
    {
        foreach (var player in PlayerController.players) {
            if (playerToTeleport != null && player.gameObject != playerToTeleport)
                continue;
            float offset = PlayerController.players.IndexOf(player) * 3.5f;
            Vector3 newPosition = new Vector3(
                    currentCheckpoint.x + offset,
                    currentCheckpoint.y + 1.2f,
                    currentCheckpoint.z
                );
            if (player.TryGetComponent(out Rigidbody rb)) { 
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.position = newPosition;
            }
            player.transform.position = newPosition;
        }
    }
    
    /// <summary>
    /// Generates and caches a hash code for the given scene name
    /// </summary>
    private static int GetSceneHash(string sceneName)
    {
        if (!sceneHashCache.TryGetValue(sceneName, out int hash))
        {
            hash = sceneName.GetHashCode();
            sceneHashCache[sceneName] = hash;
        }
        return hash;
    }
} 