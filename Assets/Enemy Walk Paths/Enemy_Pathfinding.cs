using UnityEngine;

[CreateAssetMenu(fileName = "Enemy_Pathfinding", menuName = "New EnemyPathConfig")]
public class Enemy_Pathfinding : ScriptableObject
{
    [SerializeField] Transform[] pathPrefab;
    [SerializeField] float enemyWalkSpeed = 5f;

    public Transform GetStartPoint()
    {
        return pathPrefab.GetChild(0);
    }

    public float GetEnemyMoveSpeed()
    {
        return enemyWalkSpeed;
    }

    public Transform[] GetPoints()
    {
        Transform[] waypoints = new Transform[pathPrefab.childCount];

        for (int i = 0; i < 10; i++)
        {
            waypoints[i] = pathPrefab.GetChild(i);
        }

        return waypoints;
    }
}
