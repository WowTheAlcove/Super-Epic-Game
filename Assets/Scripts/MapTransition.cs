using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapTransition : MonoBehaviour
{
    [SerializeField] private PolygonCollider2D newMapBoundary;
    [SerializeField] private CinemachineConfiner confiner;
    [SerializeField] private Direction direction;
    private enum Direction {
        None,
        Up,
        Down,
        Left,
        Right
    }
    [SerializeField] private float amtToMove = 1f;

    private void OnTriggerEnter2D(Collider2D collision) {
        if(collision.gameObject.TryGetComponent<PlayerController>(out PlayerController player)) {//if the thing entering has Player script
            confiner.m_BoundingShape2D = newMapBoundary;
            TeleportPlayerPastWaypoint(player.gameObject);
        }
    }

    private void TeleportPlayerPastWaypoint(GameObject player) {
        Vector3 newPos = player.transform.position;
        switch(direction) {
            default:
                Debug.LogError("MapTransition direction is set to None, no movement will occur.");
                return;
            case Direction.Up:
                newPos.y += amtToMove;
                break;
            case Direction.Down:
                newPos.y -= amtToMove;
                break;
            case Direction.Left:
                newPos.x -= amtToMove;
                break;
            case Direction.Right:
                newPos.x += amtToMove;
                break;
        }
        player.transform.position = newPos;
    }
}
