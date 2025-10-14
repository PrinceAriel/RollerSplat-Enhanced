using System.Collections.Generic;
using UnityEngine;

public class LevelCreator : MonoBehaviour
{
    [Header("Level Generation")]
    public GameObject groundPiecePrefab;
    public GameObject wallPrefab;
    public GameObject ballPrefab;

    [Header("Level Size")]
    public int gridWidth = 5;
    public int gridHeight = 5;
    public float tileSize = 1f;

    [Header("Level Patterns")]
    public LevelPattern[] patterns;

    [Header("Difficulty")]
    [Range(0, 1)]
    public float wallDensity = 0.2f; // Percentage of tiles that are walls

    private Transform levelContainer;

    [System.Serializable]
    public class LevelPattern
    {
        public string patternName;
        public bool[,] layout; // True = ground, False = wall
        public Vector2Int ballStartPos;
    }

    // Call this from editor or at runtime to generate a level
    [ContextMenu("Generate Random Level")]
    public void GenerateRandomLevel()
    {
        ClearLevel();
        CreateLevelContainer();

        bool[,] grid = GenerateGrid();
        Vector2Int ballPos = FindSuitableBallPosition(grid);

        BuildLevel(grid, ballPos);
    }

    [ContextMenu("Generate Easy Level")]
    public void GenerateEasyLevel()
    {
        ClearLevel();
        CreateLevelContainer();

        // Simple cross pattern
        bool[,] grid = new bool[gridWidth, gridHeight];
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                // Create a cross pattern
                grid[x, y] = (x == gridWidth / 2 || y == gridHeight / 2);
            }
        }

        Vector2Int ballPos = new Vector2Int(gridWidth / 2, gridHeight / 2);
        BuildLevel(grid, ballPos);
    }

    [ContextMenu("Generate Spiral Level")]
    public void GenerateSpiralLevel()
    {
        ClearLevel();
        CreateLevelContainer();

        bool[,] grid = new bool[gridWidth, gridHeight];

        // Create a spiral pattern
        int centerX = gridWidth / 2;
        int centerY = gridHeight / 2;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                int dx = x - centerX;
                int dy = y - centerY;
                float angle = Mathf.Atan2(dy, dx);
                float distance = Mathf.Sqrt(dx * dx + dy * dy);

                // Create spiral based on angle and distance
                float spiralValue = (angle + distance * 0.5f) % (Mathf.PI * 2);
                grid[x, y] = spiralValue < Mathf.PI;
            }
        }

        Vector2Int ballPos = new Vector2Int(centerX, centerY);
        BuildLevel(grid, ballPos);
    }

    [ContextMenu("Generate Maze Level")]
    public void GenerateMazeLevel()
    {
        ClearLevel();
        CreateLevelContainer();

        bool[,] grid = GenerateMaze(gridWidth, gridHeight);
        Vector2Int ballPos = FindSuitableBallPosition(grid);

        BuildLevel(grid, ballPos);
    }

    private bool[,] GenerateGrid()
    {
        bool[,] grid = new bool[gridWidth, gridHeight];

        // Fill with ground pieces
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                grid[x, y] = Random.value > wallDensity;
            }
        }

        // Ensure at least one path exists
        EnsureConnectedGrid(grid);

        return grid;
    }

    private void EnsureConnectedGrid(bool[,] grid)
    {
        // Simple flood fill to ensure connectivity
        // Start from center and make sure we can reach at least 60% of the grid

        int centerX = gridWidth / 2;
        int centerY = gridHeight / 2;
        grid[centerX, centerY] = true;

        List<Vector2Int> openList = new List<Vector2Int>();
        openList.Add(new Vector2Int(centerX, centerY));

        int minTiles = (int)(gridWidth * gridHeight * 0.6f);
        int currentTiles = 1;

        while (openList.Count > 0 && currentTiles < minTiles)
        {
            Vector2Int current = openList[Random.Range(0, openList.Count)];
            openList.Remove(current);

            // Check neighbors
            Vector2Int[] neighbors = {
                current + Vector2Int.up,
                current + Vector2Int.down,
                current + Vector2Int.left,
                current + Vector2Int.right
            };

            foreach (Vector2Int neighbor in neighbors)
            {
                if (neighbor.x >= 0 && neighbor.x < gridWidth &&
                    neighbor.y >= 0 && neighbor.y < gridHeight)
                {
                    if (!grid[neighbor.x, neighbor.y] && Random.value > 0.3f)
                    {
                        grid[neighbor.x, neighbor.y] = true;
                        openList.Add(neighbor);
                        currentTiles++;
                    }
                    else if (grid[neighbor.x, neighbor.y] && !openList.Contains(neighbor))
                    {
                        openList.Add(neighbor);
                    }
                }
            }
        }
    }

    private bool[,] GenerateMaze(int width, int height)
    {
        // Simple maze generation using recursive backtracking
        bool[,] grid = new bool[width, height];

        // Start with all walls
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                grid[x, y] = false;

        // Carve paths
        Stack<Vector2Int> stack = new Stack<Vector2Int>();
        Vector2Int start = new Vector2Int(0, 0);
        grid[start.x, start.y] = true;
        stack.Push(start);

        Vector2Int[] directions = {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };

        while (stack.Count > 0)
        {
            Vector2Int current = stack.Peek();
            List<Vector2Int> unvisitedNeighbors = new List<Vector2Int>();

            foreach (Vector2Int dir in directions)
            {
                Vector2Int neighbor = current + dir;
                if (neighbor.x >= 0 && neighbor.x < width &&
                    neighbor.y >= 0 && neighbor.y < height &&
                    !grid[neighbor.x, neighbor.y])
                {
                    unvisitedNeighbors.Add(neighbor);
                }
            }

            if (unvisitedNeighbors.Count > 0)
            {
                Vector2Int next = unvisitedNeighbors[Random.Range(0, unvisitedNeighbors.Count)];
                grid[next.x, next.y] = true;
                stack.Push(next);
            }
            else
            {
                stack.Pop();
            }
        }

        return grid;
    }

    private Vector2Int FindSuitableBallPosition(bool[,] grid)
    {
        // Find a ground tile, preferably near center
        int centerX = gridWidth / 2;
        int centerY = gridHeight / 2;

        // Try center first
        if (grid[centerX, centerY])
            return new Vector2Int(centerX, centerY);

        // Spiral out from center
        for (int radius = 1; radius < Mathf.Max(gridWidth, gridHeight); radius++)
        {
            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                for (int y = centerY - radius; y <= centerY + radius; y++)
                {
                    if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight)
                    {
                        if (grid[x, y])
                            return new Vector2Int(x, y);
                    }
                }
            }
        }

        return new Vector2Int(0, 0);
    }

    private void BuildLevel(bool[,] grid, Vector2Int ballPos)
    {
        // Create ground pieces and walls
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector3 pos = new Vector3(x * tileSize, 0, y * tileSize);

                if (grid[x, y])
                {
                    // Ground piece
                    GameObject ground = Instantiate(groundPiecePrefab, pos, Quaternion.identity, levelContainer);
                    ground.name = $"Ground_{x}_{y}";
                }
                else
                {
                    // Wall
                    if (wallPrefab != null)
                    {
                        GameObject wall = Instantiate(wallPrefab, pos + Vector3.up * 0.5f, Quaternion.identity, levelContainer);
                        wall.name = $"Wall_{x}_{y}";
                    }
                }
            }
        }

        // Place ball
        Vector3 ballPosition = new Vector3(ballPos.x * tileSize, 0.5f, ballPos.y * tileSize);
        GameObject ball = Instantiate(ballPrefab, ballPosition, Quaternion.identity);
        ball.name = "Ball";

        // Center camera
        Camera.main.transform.position = new Vector3(
            (gridWidth * tileSize) / 2f,
            Mathf.Max(gridWidth, gridHeight) * 1.5f,
            (gridHeight * tileSize) / 2f - Mathf.Max(gridWidth, gridHeight)
        );
    }

    private void CreateLevelContainer()
    {
        GameObject container = new GameObject("Level");
        levelContainer = container.transform;
    }

    private void ClearLevel()
    {
        // Clear existing level
        GameObject existingLevel = GameObject.Find("Level");
        if (existingLevel != null)
        {
            DestroyImmediate(existingLevel);
        }

        // Clear ball
        GameObject existingBall = GameObject.Find("Ball");
        if (existingBall != null)
        {
            DestroyImmediate(existingBall);
        }
    }

    // Helper method to save level as a scene or prefab
    public void SaveCurrentLevel(string levelName)
    {
        // This would require editor scripting to properly save
        Debug.Log($"Level '{levelName}' layout generated. Use Unity's prefab system to save it.");
    }
}
