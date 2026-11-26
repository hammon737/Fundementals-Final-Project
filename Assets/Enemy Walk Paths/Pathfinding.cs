using UnityEngine;

public class Pathfinding : MonoBehaviour
{
    [SerializeField] EnemyPathSO enemyPath;
    Transform[] waypoints;
    int waypointIndex = 0;

    void Start()
    {
        waypoints = enemyPath.GetWaypoints();
        transform.position = enemyPath.GetStartingWaypoint().position;
    }

    void Update()
    {
        FollowPath();
    }

    void FollowPath()
    {
        if (waypointIndex < waypoints.Length)
        {
            Vector3 targetPosition = waypoints[waypointIndex].position;
            float moveDelta = enemyPath.GetEnemyMoveSpeed() * Time.deltaTime;
            transform.position = Vector2.MoveTowards(transform.position, targetPosition, moveDelta);
            if (transform.position == targetPosition)
            {
                waypointIndex++;
            }
            if (waypointIndex >= waypoints.Length)
            {
                waypointIndex = 0;
            }
        }
        
    }

}
