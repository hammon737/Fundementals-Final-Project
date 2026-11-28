using UnityEngine;

public class Pathfinding : MonoBehaviour
{
    [SerializeField] EnemyPathSO enemyPath;
    Transform[] waypoints;
    int waypointIndex = 0;
    public Animator animator;
    private Vector3 currentPos;

    private void Start()
    {
        waypoints = enemyPath.GetWaypoints();
        transform.position = enemyPath.GetStartingWaypoint().position;
        animator = GetComponent<Animator>();
        currentPos = transform.position;
    }

    void Update()
    {
        FollowPath();
        UpdateAnimation();
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

    void UpdateAnimation()
    {
        Vector3 movement = transform.position - currentPos;

        if (Mathf.Abs(movement.x) == Mathf.Abs(movement.y))
        {
            return;
        }
        else if (Mathf.Abs(movement.x) < Mathf.Abs(movement.y))
        {
            if (movement.y > 0)
            {
                animator.SetBool("IsForward", false);
                animator.SetBool("IsLeft", false);
                animator.SetBool("IsRight", false);
                animator.SetBool("IsBack", true);
            }
            else
            {
                animator.SetBool("IsBack", false);
                animator.SetBool("IsLeft", false);
                animator.SetBool("IsRight", false);
                animator.SetBool("IsForward", true);
            }
        }
        else if (Mathf.Abs(movement.x) > Mathf.Abs(movement.y))
        {
            if (movement.x > 0)
            {
                animator.SetBool("IsRight", true);
                animator.SetBool("IsLeft", false);
                animator.SetBool("IsForward", false);
                animator.SetBool("IsBack", false);
            }
            else
            {
                animator.SetBool("IsLeft", true);
                animator.SetBool("IsRight", false);
                animator.SetBool("IsForward", false);
                animator.SetBool("IsBack", false);
            }

        }
        

        currentPos = transform.position;
    }

}
