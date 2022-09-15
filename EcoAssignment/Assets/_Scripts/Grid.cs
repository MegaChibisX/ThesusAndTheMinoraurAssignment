using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class Grid : MonoBehaviour
{

    // Sets the current grid as an instance.
    public static Grid instance;

    public string nextStageName = "Stage1";
    private bool gameEnded = false;

    // The size of the stage
    public Vector2Int stageSize = new Vector2Int(20, 15);
    public Vector2Int playerSpawn = new Vector2Int(1, 0);
    public Vector2Int enemySpawn = new Vector2Int(-1, 0);

    [HideInInspector]
    public Vector2Int playerPos = new Vector2Int(1, 0);
    [HideInInspector]
    public Vector2Int enemyPos = new Vector2Int(-1, 0);

    private bool playerWaits = false;
    private bool justUndid = false;

    // The size of each grid object.
    public float blockSize = 1;
    //[HideInInspector]
    public Vector2 corner;

    public GameObject objPlayer;
    public GameObject objEnemy;

    // Types of blocks for the grid.
    public enum Block { Empty, Wall }
    public BlockState[] grid;

    // Materials for visuals
    public Material matFloor;
    public Material matWall;
    public Material matFinish;

    public Material matPlayer;
    public Material matEnemy;

    // The visuals
    public GameObject canvasVic;
    public GameObject canvasLoss;

    // The undo history
    public List<UndoStep> undoHistory;

    public Vector2Int[] solvePath;
    public Vector2Int[] solvePathEnemy;

    public Transform pathParent;

    // Class for block state
    [System.Serializable]
    public class BlockState
    {
        public bool leftBlocked, rightBlocked, upBlocked, downBlocked;

        public bool isFinish;
        public BlockState()
        {

        }
    }
    [System.Serializable]
    public class UndoStep
    {
        public Vector2Int playerPos;
        public Vector2Int enemyPos;
        public UndoStep(Vector2Int playerSteps, Vector2Int enemySteps)
        {
            this.playerPos = playerSteps;
            this.enemyPos = enemySteps;
        }
    }

    public void Start()
    {
        // Makes the grid a singleton
        instance = this;
        pathParent = new GameObject("Path parent").transform;
        pathParent.parent = transform;
        pathParent.transform.localPosition = Vector3.zero;

        // Sets the current positions as the original onces.
        playerPos = playerSpawn;
        enemyPos = enemySpawn;

        // Sets the visual positions
        objPlayer.transform.position = (-corner + playerPos) * blockSize;
        objEnemy.transform.position = (-corner + enemyPos) * blockSize;

        // Searches for the victory and defeat graphics.
        canvasVic = GameObject.Find("Canvas").transform.Find("Victory").gameObject;
        canvasLoss = GameObject.Find("Canvas").transform.Find("Defeat").gameObject;

        Button button = GameObject.Find("Canvas").transform.Find("Undo").GetComponent<Button>();
        button.onClick.AddListener(() => { Undo(); });

        button = GameObject.Find("Canvas").transform.Find("Wait").GetComponent<Button>();
        button.onClick.AddListener(() => { Wait(); });

        button = GameObject.Find("Canvas").transform.Find("Restart").GetComponent<Button>();
        button.onClick.AddListener(() => { Restart(); });

        button = GameObject.Find("Canvas").transform.Find("Solve").GetComponent<Button>();
        button.onClick.AddListener(() => { SolveMaze(); });

        // Starts the game loop
        undoHistory = new List<UndoStep>();
        StartCoroutine(GameLoop());
    }
    public void Update()
    {
        UpdatePositions();
    }
    public void OnDrawGizmos()
    {
        if (solvePath == null || solvePath.Length < 2)
            return;

        Gizmos.color = Color.green;

        Gizmos.DrawSphere(-corner + (Vector2)solvePath[0], blockSize * 0.35f);
        for (int i = 1; i < solvePath.Length; i++)
        {
            Gizmos.DrawSphere(-corner + (Vector2)solvePath[i], blockSize * 0.35f);
            Gizmos.DrawLine(-corner + (Vector2)solvePath[i - 1], -corner + (Vector2)solvePath[i]);
        }

        Gizmos.color = Color.red;

        Gizmos.DrawSphere(-corner + (Vector2)solvePathEnemy[0], blockSize * 0.25f);
        for (int i = 1; i < solvePathEnemy.Length; i++)
        {
            Gizmos.DrawSphere(-corner + (Vector2)solvePathEnemy[i], blockSize * 0.25f);
            Gizmos.DrawLine(-corner + (Vector2)solvePathEnemy[i - 1], -corner + (Vector2)solvePathEnemy[i]);
        }
    }


    // Generates an empty grid for the labyrinth.
    public void GenerateGrid()
    {
        // Generates an empty grid with walls
        grid = new BlockState[stageSize.x * stageSize.y];

        for (int x = 0; x < stageSize.x; x++)
        {
            for (int y = 0; y < stageSize.y; y++)
            {
                int index = V2ToInt(x, y);
                grid[index] = new BlockState();

                // Makes walls for each state1
                if (x == 0)
                    grid[index].leftBlocked = true;
                else if (x == stageSize.x - 1)
                    grid[index].rightBlocked = true;
                if (y == 0)
                    grid[index].downBlocked = true;
                else if (y == stageSize.y - 1)
                    grid[index].upBlocked = true;


            }
        }
    }

    // Generates the visual elements of the stage
    public void GenerateMesh()
    {
        // Checks if the grid size is invalid.
        if (grid == null ||
            grid.Length != stageSize.y * stageSize.x)
        {
            Debug.LogError("The grid you want to visualize doesn't match the stage size you have defined!");
            return;
        }

        // Deletes previous grid objects
        Transform[] forDeletion = transform.GetComponentsInChildren<Transform>();
        foreach (Transform t in forDeletion)
        {
            if (t == transform || t == null)
                continue;
            DestroyImmediate(t.gameObject);

        }

        // Generates some primitives for the terrain.
        corner = (Vector2)stageSize * 0.5f * blockSize;

        for (int x = 0; x < stageSize.x; x++)
        {
            for (int y =0; y < stageSize.y; y++)
            {
                int index = V2ToInt(x, y);

                // Creates a cube for the grid position.
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.parent = transform;
                cube.transform.localPosition = -(Vector3)corner + new Vector3(x, y) * blockSize + Vector3.forward * 0.4f * blockSize;
                cube.transform.localScale = Vector3.one * blockSize * 0.95f;
                cube.transform.localScale = Vector3.Scale(new Vector3(1, 1, 0.2f), cube.transform.localScale);

                cube.GetComponent<MeshRenderer>().material = matWall;
                DestroyImmediate(cube.GetComponent<Collider>());

                // Makes the block flat if it is an empty block
                if (grid[index].leftBlocked)
                {
                    GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    wall.transform.parent = cube.transform;
                    wall.transform.localScale = Vector3.Scale(new Vector3(0.2f, 1, 1), wall.transform.localScale);
                    wall.transform.localPosition = -Vector3.forward * 0.4f * blockSize + Vector3.left * 0.4f;

                    wall.GetComponent<MeshRenderer>().material = matFloor;
                    DestroyImmediate(wall.GetComponent<Collider>());
                }
                if (grid[index].rightBlocked)
                {
                    GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    wall.transform.parent = cube.transform;
                    wall.transform.localScale = Vector3.Scale(new Vector3(0.2f, 1, 1), wall.transform.localScale);
                    wall.transform.localPosition = -Vector3.forward * 0.4f * blockSize + Vector3.right * 0.4f;

                    wall.GetComponent<MeshRenderer>().material = matFloor;
                    DestroyImmediate(wall.GetComponent<Collider>());
                }
                if (grid[index].upBlocked)
                {
                    GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    wall.transform.parent = cube.transform;
                    wall.transform.localScale = Vector3.Scale(new Vector3(1, 0.2f, 1), wall.transform.localScale);
                    wall.transform.localPosition = -Vector3.forward * 0.4f * blockSize + Vector3.up* 0.4f;

                    wall.GetComponent<MeshRenderer>().material = matFloor;
                    DestroyImmediate(wall.GetComponent<Collider>());
                }
                if (grid[index].downBlocked)
                {
                    GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    wall.transform.parent = cube.transform;
                    wall.transform.localScale = Vector3.Scale(new Vector3(1, 0.2f, 1), wall.transform.localScale);
                    wall.transform.localPosition = -Vector3.forward * 0.4f * blockSize + Vector3.down * 0.4f;

                    wall.GetComponent<MeshRenderer>().material = matFloor;
                    DestroyImmediate(wall.GetComponent<Collider>());
                }

                // Generates a small cube over the finish line.
                if (grid[index].isFinish)
                {
                    GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    wall.transform.parent = cube.transform;
                    wall.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
                    wall.transform.localPosition = -Vector3.forward * 0.4f * blockSize;

                    wall.GetComponent<MeshRenderer>().material = matFinish;
                }
            }
        }

        // Generates player and enemy objects
        objPlayer = GameObject.CreatePrimitive(PrimitiveType.Cube);
        objPlayer.transform.parent = transform;
        objPlayer.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        objPlayer.transform.localPosition = -corner + (Vector2)playerSpawn * blockSize;
        objPlayer.GetComponent<MeshRenderer>().material = matPlayer;
        playerPos = playerSpawn;

        objEnemy = GameObject.CreatePrimitive(PrimitiveType.Cube);
        objEnemy.transform.parent = transform;
        objEnemy.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
        objEnemy.transform.localPosition = -corner + (Vector2)enemySpawn * blockSize;
        objEnemy.GetComponent<MeshRenderer>().material = matEnemy;
        enemyPos = enemySpawn;


        // Sets camera position to match the grid size.
        Camera cmr = FindObjectOfType<Camera>();

        // Using A/sin(a) == B/sin(b), the maximum distance is found
        float distanceY = Mathf.Sin(cmr.fieldOfView * 0.5f * Mathf.Deg2Rad) * stageSize.y / Mathf.Sin((180f - (cmr.fieldOfView * 0.5f) * Mathf.Deg2Rad));
        float distanceX = Mathf.Sin(cmr.fieldOfView * 0.5f * Mathf.Deg2Rad) * stageSize.x / Mathf.Sin((180f - (cmr.fieldOfView * 0.5f) * Mathf.Deg2Rad));

        cmr.transform.position = transform.position + Vector3.forward * Mathf.Min(distanceX, distanceY);
    }

    // Makes the player and enemy smoothly move to their target destinations
    public void UpdatePositions()
    {
        objPlayer.transform.position = Vector3.MoveTowards(objPlayer.transform.position, (-corner + playerPos) * blockSize, Time.deltaTime * 10f);
        objEnemy.transform.position = Vector3.Lerp(objEnemy.transform.position, (-corner + enemyPos) * blockSize, Time.deltaTime * 10f);
    }

    // The main game loop, which allows the player to move once, then the enemy to move twice.
    public IEnumerator GameLoop(bool recordFirstUndo = true)
    {
        Vector2Int indexTarget;
        yield return null;

        while (true)
        {
            // Keeps track of undo history
            justUndid = false;

            // Player turn.
            // Make sure the player doesn't accidentally skip their turn.
            playerWaits = false;
            while (true)
            {

                if (Input.GetKeyDown(KeyCode.DownArrow) && playerPos.y > 0)
                {
                    indexTarget = playerPos + Vector2Int.down;
                    if (!grid[V2ToInt(indexTarget)].upBlocked &&
                        !grid[V2ToInt(playerPos)].downBlocked)
                    {
                        undoHistory.Add(new UndoStep(playerPos, enemyPos));
                        playerPos.y--;
                        break;
                    }
                }
                else if (Input.GetKeyDown(KeyCode.UpArrow) && playerPos.y < stageSize.y - 1)
                {
                    indexTarget = playerPos + Vector2Int.up;
                    if (!grid[V2ToInt(indexTarget)].downBlocked &&
                        !grid[V2ToInt(playerPos)].upBlocked)
                    {
                        undoHistory.Add(new UndoStep(playerPos, enemyPos));
                        playerPos.y++;
                        break;
                    }
                }
                else if (Input.GetKeyDown(KeyCode.LeftArrow) && playerPos.x > 0)
                {
                    indexTarget = playerPos + Vector2Int.left;
                    if (!grid[V2ToInt(indexTarget)].rightBlocked &&
                        !grid[V2ToInt(playerPos)].leftBlocked)
                    {
                        undoHistory.Add(new UndoStep(playerPos, enemyPos));
                        playerPos.x--;
                        break;
                    }
                }
                else if (Input.GetKeyDown(KeyCode.RightArrow) && playerPos.x < stageSize.x - 1)
                {
                    indexTarget = playerPos + Vector2Int.right;
                    if (!grid[V2ToInt(indexTarget)].leftBlocked &&
                        !grid[V2ToInt(playerPos)].rightBlocked)
                    {
                        undoHistory.Add(new UndoStep(playerPos, enemyPos));
                        playerPos.x++;
                        break;
                    }
                }

                if (playerWaits)
                {
                    undoHistory.Add(new UndoStep(playerPos, enemyPos));
                    break;
                }

                yield return null;
            }
            if (grid[V2ToInt(playerPos)].isFinish)
            {
                yield return CorVictory();
            }
            yield return new WaitForSeconds(0.2f);


            // Enemy turn.
            float enemyTurnDelay = 0.5f;
            for (int i = 0; i < 2; i++)
            {
                if (playerPos.x != enemyPos.x)
                {
                    if (playerPos.x > enemyPos.x)
                    {
                        indexTarget = enemyPos + Vector2Int.right;
                        if (!grid[V2ToInt(indexTarget)].leftBlocked &&
                            !grid[V2ToInt(enemyPos)].rightBlocked)
                        {
                            enemyPos = indexTarget;
                            yield return new WaitForSeconds(enemyTurnDelay);
                        }
                    }
                    else if (playerPos.x < enemyPos.x)
                    {
                        indexTarget = enemyPos + Vector2Int.left;
                        if (!grid[V2ToInt(indexTarget)].rightBlocked &&
                            !grid[V2ToInt(enemyPos)].leftBlocked)
                        {
                            enemyPos = indexTarget;
                            yield return new WaitForSeconds(enemyTurnDelay);
                        }
                    }
                }
                else
                {
                    if (playerPos.y > enemyPos.y)
                    {
                        indexTarget = enemyPos + Vector2Int.up;
                        if (!grid[V2ToInt(indexTarget)].downBlocked &&
                            !grid[V2ToInt(enemyPos)].upBlocked)
                        {
                            enemyPos = indexTarget;
                            yield return new WaitForSeconds(enemyTurnDelay);
                        }
                    }
                    else if (playerPos.y < enemyPos.y)
                    {
                        indexTarget = enemyPos + Vector2Int.down;
                        if (!grid[V2ToInt(indexTarget)].upBlocked &&
                            !grid[V2ToInt(enemyPos)].downBlocked)
                        {
                            enemyPos = indexTarget;
                            yield return new WaitForSeconds(enemyTurnDelay);
                        }
                    }
                }

                if (enemyPos == playerPos)
                {
                    yield return CorDefeat();
                }
            }

            yield return null;
        }
    }


    // Waits for the player input to move to the next room
    public IEnumerator CorVictory()
    {
        canvasVic.SetActive(true);
        gameEnded = true;

        while (!Input.GetKeyDown(KeyCode.Space))
            yield return null;

        SceneManager.LoadScene(nextStageName);
    }
    public IEnumerator CorDefeat()
    {
        canvasLoss.SetActive(true);
        gameEnded = true;

        while (!Input.GetKeyDown(KeyCode.Space))
            yield return null;

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }


    // Undo state
    public static void Undo()
    {
        if (instance.gameEnded || instance.undoHistory.Count <= 0 || instance.justUndid)
            return;

        // Stops the current loop
        instance.StopAllCoroutines();

        UndoStep step = instance.undoHistory[instance.undoHistory.Count - 1];
        instance.undoHistory.RemoveAt(instance.undoHistory.Count - 1);
        instance.justUndid = true;

        // Sets the positions to the last move.
        instance.playerPos = step.playerPos;
        instance.enemyPos = step.enemyPos;

        // Start the game loop again.
        instance.StartCoroutine(instance.GameLoop(false));
    }
    public static void Wait()
    {
        if (instance.gameEnded)
            return;

        instance.playerWaits = true;
    }
    public static void Restart()
    {
        if (instance.gameEnded)
            return;

        instance.StopAllCoroutines();
        instance.StartCoroutine(instance.CorDefeat());
    }
    public static void SolveMaze()
    {
        if (instance.gameEnded)
            return;

        foreach (Transform t in instance.pathParent)
        {
            if (t != instance.pathParent)
                Destroy(t.gameObject);
        }

        // Does a bit of pathfinding.
        Solver solver = new Solver();
        solver.SolveMaze(instance);

        foreach (Vector2Int i in instance.solvePath)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.parent = instance.pathParent;
            cube.transform.localPosition = -instance.corner + instance.blockSize * (Vector2)i;
            cube.transform.localScale = instance.blockSize * 0.2f * Vector3.one;
            cube.GetComponent<MeshRenderer>().material = instance.matPlayer;
        }
        foreach (Vector2Int i in instance.solvePathEnemy)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.parent = instance.pathParent;
            cube.transform.localPosition = -instance.corner + instance.blockSize * (Vector2)i;
            cube.transform.localScale = instance.blockSize * 0.15f * Vector3.one;
            cube.GetComponent<MeshRenderer>().material = instance.matEnemy;
        }

    }




    // Gets the grid index of a coordinate 
    public int V2ToInt(Vector2Int v2)
    {
        return v2.y * stageSize.x + v2.x;
    }
    public int V2ToInt(int x, int y)
    {
        return y * stageSize.x + x;
    }


    public Vector2Int GetFinishPoint()
    {
        for (int x = 0; x < stageSize.x; x++)
        {
            for (int y = 0; y < stageSize.y; y++)
            {
                if (grid[V2ToInt(x, y)].isFinish)
                    return new Vector2Int(x, y);
            }
        }

        return new Vector2Int(-1, -1);
    }

}
