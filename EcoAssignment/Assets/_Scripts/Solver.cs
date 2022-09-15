using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Solver : MonoBehaviour
{
    // Solves the problem using an A* algorithm

    public static Vector2Int startPos;
    public static Vector2Int endPos;

    public class EnemyPathHistory
    {
        public List<Vector2Int> path;
        public EnemyPathHistory()
        {
            path = new List<Vector2Int>();
        }
        public EnemyPathHistory(Vector2Int[] oldPath, Vector2Int newPos)
        {
            path = oldPath.ToList();
            path.Add(newPos);
        }
    }
    public class Node : IEqualityComparer<Node>
    {
        public Vector2Int playerPos;
        public Vector2Int enemyPos;
        public EnemyPathHistory enmPath;

        public Node prevNode;

        public float fCost, gCost;
        public float hCost
        {
            get { return fCost + gCost; }
        }

        public Node(float fCost, Vector2Int playerPos, Vector2Int enemyPos, EnemyPathHistory pathHistory, Node prevNode)
        {
            this.fCost = fCost;
            this.gCost = Vector2Int.Distance(playerPos, endPos);

            this.playerPos = playerPos;
            this.enemyPos = enemyPos;
            this.enmPath = pathHistory;
            this.prevNode = prevNode;
        }

        public bool IsBetterThan(Node other)
        {
            if (gCost <= other.gCost)
                return true;

            return hCost < hCost;
        }

        public bool Equals(Node node1, Node node2)
        {
            if (node1 == null || node2 == null)
                return false;
            return node1.playerPos == node2.playerPos;
        }
        public int GetHashCode(Node node)
        {
            if (node == null)
                return 0;

            return node.GetHashCode();
        }
    }


    public void SolveMaze(Grid grid)
    {
        // Initializes the A* pathfind.
        startPos = grid.playerSpawn;
        endPos = grid.GetFinishPoint();

        // The open group is all nodes near the current path, that can be checked.
        List<Node> openGroup = new List<Node>();
        // The closed group is all the nodes that have already been checked and judged invalid.
        HashSet<Vector2Int> closedGroup = new HashSet<Vector2Int>();
        // The current node
        Node currentNode = new Node(0, grid.playerPos, grid.enemyPos, new EnemyPathHistory(), null);
        openGroup.Add(currentNode);

        int steps = 0;
        // While there are nodes to explore.
        while (openGroup.Count > 0)
        {
            steps++;
            if (steps > 10000)
            {
                Debug.Log("TOO MANY STEPS");
                break;
            }
            // Removes one node from the open group and makes it the current one.
            currentNode = openGroup[0];
            closedGroup.Add(openGroup[0].playerPos);
            openGroup.RemoveAt(0);

            // If the current node is at the finish line, then we win!
            if (currentNode.playerPos == endPos)
                break;

            // Gets all node neighbours.
            List<Node> neighbours = new List<Node>();
            if (currentNode.playerPos.x > 0 &&
                !grid.grid[grid.V2ToInt(currentNode.playerPos + Vector2Int.left)].rightBlocked &&
                !grid.grid[grid.V2ToInt(currentNode.playerPos)].leftBlocked)
            {
                Vector2Int newEnmPos = PerformEnemyTurn(grid, currentNode.playerPos + Vector2Int.left, currentNode.enemyPos, 2);
                Node newNode = new Node(currentNode.fCost + 1, currentNode.playerPos + Vector2Int.left, newEnmPos, new EnemyPathHistory(currentNode.enmPath.path.ToArray(), newEnmPos), currentNode);

                if (!closedGroup.Contains(newNode.playerPos) && newNode.playerPos != newEnmPos)
                    neighbours.Add(newNode);
            }
            if (currentNode.playerPos.x < grid.stageSize.x - 1 &&
                !grid.grid[grid.V2ToInt(currentNode.playerPos + Vector2Int.right)].leftBlocked &&
                !grid.grid[grid.V2ToInt(currentNode.playerPos)].rightBlocked)
            {
                Vector2Int newEnmPos = PerformEnemyTurn(grid, currentNode.playerPos + Vector2Int.right, currentNode.enemyPos, 2);
                Node newNode = new Node(currentNode.fCost + 1, currentNode.playerPos + Vector2Int.right, newEnmPos, new EnemyPathHistory(currentNode.enmPath.path.ToArray(), newEnmPos), currentNode);

                if (!closedGroup.Contains(newNode.playerPos) && newNode.playerPos != newEnmPos)
                    neighbours.Add(newNode);
            }
            if (currentNode.playerPos.y > 0 &&
                !grid.grid[grid.V2ToInt(currentNode.playerPos + Vector2Int.down)].upBlocked &&
                !grid.grid[grid.V2ToInt(currentNode.playerPos)].downBlocked)
            {
                Vector2Int newEnmPos = PerformEnemyTurn(grid, currentNode.playerPos + Vector2Int.down, currentNode.enemyPos, 2);
                Node newNode = new Node(currentNode.fCost + 1, currentNode.playerPos + Vector2Int.down, newEnmPos, new EnemyPathHistory(currentNode.enmPath.path.ToArray(), newEnmPos), currentNode);

                if (!closedGroup.Contains(newNode.playerPos) && newNode.playerPos != newEnmPos)
                    neighbours.Add(newNode);
            }
            if (currentNode.playerPos.y < grid.stageSize.y - 1 &&
                !grid.grid[grid.V2ToInt(currentNode.playerPos + Vector2Int.up)].downBlocked &&
                !grid.grid[grid.V2ToInt(currentNode.playerPos)].upBlocked)
            {
                Vector2Int newEnmPos = PerformEnemyTurn(grid, currentNode.playerPos + Vector2Int.up, currentNode.enemyPos, 2);
                Node newNode = new Node(currentNode.fCost + 1, currentNode.playerPos + Vector2Int.up, newEnmPos, new EnemyPathHistory(currentNode.enmPath.path.ToArray(), newEnmPos), currentNode);

                if (!closedGroup.Contains(newNode.playerPos) && newNode.playerPos != newEnmPos)
                    neighbours.Add(newNode);
            }

            // Decides where in the open set each neighbour is gonna go.
            foreach (Node n in neighbours)
            {
                for (int i = 0; i < openGroup.Count; i++)
                {
                    if (n.IsBetterThan(openGroup[i]))
                    {
                        openGroup.Insert(i, n);
                        break;
                    }
                }
                openGroup.Add(n);
            }
        }

        Debug.Log("Open Group Left: " + openGroup.Count);

        // Returns the solved path, backwards.
        Vector2Int[] solvedEnemy = currentNode.enmPath.path.ToArray();

        List<Vector2Int> solveList = new List<Vector2Int>();
        while (currentNode != null)
        {
            solveList.Add(currentNode.playerPos);
            currentNode = currentNode.prevNode;
        }
        
        grid.solvePath = solveList.ToArray();
        grid.solvePathEnemy = solvedEnemy;
    }

    public Vector2Int PerformEnemyTurn(Grid grid, Vector2Int playerPos, Vector2Int enemyPos, int loops)
    {
        Vector2Int indexTarget;

        for (int i = 0; i < loops; i++)
        {
            if (playerPos.x != enemyPos.x)
            {
                if (playerPos.x > enemyPos.x)
                {
                    indexTarget = enemyPos + Vector2Int.right;
                    if (!grid.grid[grid.V2ToInt(indexTarget)].leftBlocked &&
                        !grid.grid[grid.V2ToInt(grid.enemyPos)].rightBlocked)
                    {
                        enemyPos = indexTarget;
                    }
                }
                else if (playerPos.x < enemyPos.x)
                {
                    indexTarget = enemyPos + Vector2Int.left;
                    if (!grid.grid[grid.V2ToInt(indexTarget)].rightBlocked &&
                        !grid.grid[grid.V2ToInt(enemyPos)].leftBlocked)
                    {
                        enemyPos = indexTarget;
                    }
                }
            }
            else
            {
                if (playerPos.y > enemyPos.y)
                {
                    indexTarget = enemyPos + Vector2Int.up;
                    if (!grid.grid[grid.V2ToInt(indexTarget)].downBlocked &&
                        !grid.grid[grid.V2ToInt(enemyPos)].upBlocked)
                    {
                        enemyPos = indexTarget;
                    }
                }
                else if (playerPos.y < enemyPos.y)
                {
                    indexTarget = enemyPos + Vector2Int.down;
                    if (!grid.grid[grid.V2ToInt(indexTarget)].upBlocked &&
                        !grid.grid[grid.V2ToInt(enemyPos)].downBlocked)
                    {
                        enemyPos = indexTarget;
                    }
                }
            }
        }

        return enemyPos;
    }
}