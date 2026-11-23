using UnityEngine;

public class Pathfinding : MonoBehaviour
{
    [SerializeField] Enemy_Pathfinding enemypath;
    Transform[] waypoints;
    int waypointIndex = 0;

    
    void Start()
    {
        waypoints = enemypath.GetStartPoint();
        transform.position = enemypath.GetStartPoint().position;
    }

    
    void Update()
    {
        FollowPath();
    }

    void FollowPath()
    {
        if (waypointIndex < waypoints.Length)
        {
            Vector2 targetPos = waypoints[waypointIndex].position;
            float moveDelta = enemypath.GetEnemyMoveSpeed() * Time.deltaTime;
            transform.position = Vector2.MoveTowards(transform.position, targetPos, moveDelta);
            if (transform.position == targetPos)
            {
                waypointIndex += 1;
            }
        }
    }

}
