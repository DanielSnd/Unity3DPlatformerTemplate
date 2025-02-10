using UnityEngine;
using System;

[Serializable]
public class GameObjectSpawnAction : TriggerAction
{
    [SerializeField] private GameObject prefab;
    [SerializeField] private Transform spawnLocation;
    [SerializeField] private RotationOption rotationOption;
    [SerializeField] private bool raycastToGround = false;
    
    protected override void OnExecute()
    {
        if (prefab != null && spawnLocation != null)
        {
            Quaternion rotation = GetSpawnRotation();
            Vector3 position = spawnLocation.position;

            if (raycastToGround)
            {
                position = GetGroundPosition(spawnLocation.position);
            }

            GameObject.Instantiate(prefab, position, rotation);
        }
        Complete();
    }

    private Quaternion GetSpawnRotation()
    {
        switch (rotationOption)
        {
            case RotationOption.MatchTransformForward:
                return Quaternion.LookRotation(spawnLocation.forward);
            case RotationOption.MatchTransformUp:
                return Quaternion.LookRotation(spawnLocation.up);
            case RotationOption.MatchTransformRight:
                return Quaternion.LookRotation(spawnLocation.right);
            case RotationOption.GlobalUp:
                return Quaternion.LookRotation(Vector3.up);
            case RotationOption.GlobalForward:
                return Quaternion.LookRotation(Vector3.forward);
            case RotationOption.GlobalRight:
                return Quaternion.LookRotation(Vector3.right);
            case RotationOption.RandomRotation:
                return UnityEngine.Random.rotation;
            case RotationOption.RandomRotationY:
                return Quaternion.Euler(0, UnityEngine.Random.Range(0f, 360f), 0);
            default:
                return spawnLocation.rotation;
        }
    }

    private Vector3 GetGroundPosition(Vector3 startPosition)
    {
        RaycastHit hit;
        if (Physics.Raycast(startPosition, Vector3.down, out hit))
        {
            return hit.point;
        }
        return startPosition;
    }
}

public enum RotationOption
{
    Default,
    MatchTransformForward,
    MatchTransformUp,
    MatchTransformRight,
    GlobalUp,
    GlobalForward,
    GlobalRight,
    RandomRotation,
    RandomRotationY
}
