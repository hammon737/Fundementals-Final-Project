using UnityEngine;

public class Pathfinding : MonoBehaviour
{
    [SerializeField] EnemyPathSO enemyPath;
    Transform[] waypoints;
    int waypointIndex = 0;
    private Animator enanimator;
    private Vector3 currentPos;

    private void Start()
    {
        waypoints = enemyPath.GetWaypoints();
        transform.position = enemyPath.GetStartingWaypoint().position;
        enanimator = GetComponent<Animator>();
        currentPos = transform.position;
    }

    private void Update()
    {
        FollowPath();
        UpdateAnimation();
    }

    public void FollowPath()
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

    public void UpdateAnimation()
    {
        Vector3 movement = transform.position - currentPos;

        if (Mathf.Abs(movement.x) == Mathf.Abs(movement.z))
        {
            return;
        }
        else if (Mathf.Abs(movement.x) > Mathf.Abs(movement.z))
        {
            if (movement.x > 0)
            {
                enanimator.SetBool("IsRight", true);
                enanimator.SetBool("IsLeft", false);
                enanimator.SetBool("IsForward", false);
                enanimator.SetBool("IsBack", false);
            }
            else
            {
                enanimator.SetBool("IsLeft", true);
                enanimator.SetBool("IsRight", false);
                enanimator.SetBool("IsForward", false);
                enanimator.SetBool("IsBack", false);
            }

        }
        else if (Mathf.Abs(movement.x) < Mathf.Abs(movement.z))
        {
            if (movement.z > 0)
            {
                enanimator.SetBool("IsForward", false);
                enanimator.SetBool("IsLeft", false);
                enanimator.SetBool("IsRight", false);
                enanimator.SetBool("IsBack", true);
            }
            else
            {
                enanimator.SetBool("IsBack", false);
                enanimator.SetBool("IsLeft", false);
                enanimator.SetBool("IsRight", false);
                enanimator.SetBool("IsForward", true);
            }
        }

        currentPos = transform.position;
    }

}
