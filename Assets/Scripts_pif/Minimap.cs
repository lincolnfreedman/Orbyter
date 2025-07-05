using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class Minimap : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController_pif playerController; // Reference to the player controller
    [SerializeField] private GameObject worldGrid; // Grid object containing tilemap children
    [SerializeField] private RectTransform playerIcon; // UI object representing the player on minimap
    
    [Header("Minimap Settings")]
    [SerializeField] private float mapScale = 0.1f; // Scale factor for world-to-minimap conversion
    [SerializeField] private bool centerOnPlayer = false; // Whether to center the minimap on the player
    [SerializeField] private Vector2 mapOffset = Vector2.zero; // Manual offset for minimap positioning
    [SerializeField] private Color terrainColor = Color.gray; // Color for terrain boxes on minimap
    [SerializeField] private GameObject terrainBoxPrefab; // Prefab for terrain representation (UI Image)
    [SerializeField] private int maxTileGroupSize = 8; // Maximum size of tile groups (larger = better performance, less accuracy)
    [SerializeField] private float terrainBoxGap = 0.05f; // Small gap between terrain boxes to prevent overlap (in world units)
    
    private RectTransform minimapRect; // RectTransform of this minimap UI object
    private Tilemap[] worldTilemaps; // Array of all tilemaps in the world
    private Bounds worldBounds; // Combined bounds of all tilemaps
    private Vector2 worldSize; // Size of the world in world units
    private Vector2 minimapSize; // Size of the minimap in UI units
    private List<GameObject> terrainBoxes = new List<GameObject>();
    
    private void DebugWorldTerrainLayout()
    {
        Debug.Log("=== WORLD TERRAIN LAYOUT DEBUG ===");
        Debug.Log($"World bounds: center={worldBounds.center}, size={worldBounds.size}");
        Debug.Log($"World extends from {worldBounds.min} to {worldBounds.max}");
        
        Tilemap[] tilemaps = worldGrid.GetComponentsInChildren<Tilemap>();
        
        for (int i = 0; i < tilemaps.Length; i++)
        {
            Tilemap tilemap = tilemaps[i];
            Bounds tilemapBounds = tilemap.localBounds;
            Vector3 worldMin = tilemap.transform.TransformPoint(tilemapBounds.min);
            Vector3 worldMax = tilemap.transform.TransformPoint(tilemapBounds.max);
            
            Debug.Log($"Tilemap {i} ({tilemap.name}):");
            Debug.Log($"  Local bounds: center={tilemapBounds.center}, size={tilemapBounds.size}");
            Debug.Log($"  World position: min={worldMin}, max={worldMax}");
            Debug.Log($"  Tilemap transform position: {tilemap.transform.position}");
            
            // Check composite collider paths
            CompositeCollider2D compositeCollider = tilemap.GetComponent<CompositeCollider2D>();
            if (compositeCollider != null)
            {
                Debug.Log($"  Has {compositeCollider.pathCount} composite collider paths");
                
                for (int pathIndex = 0; pathIndex < compositeCollider.pathCount; pathIndex++)
                {
                    Vector2[] pathVertices = new Vector2[compositeCollider.GetPathPointCount(pathIndex)];
                    compositeCollider.GetPath(pathIndex, pathVertices);
                    
                    if (pathVertices.Length > 0)
                    {
                        Vector2 pathMin = pathVertices[0];
                        Vector2 pathMax = pathVertices[0];
                        
                        foreach (Vector2 vertex in pathVertices)
                        {
                            Vector2 worldVertex = tilemap.transform.TransformPoint(vertex);
                            pathMin = Vector2.Min(pathMin, worldVertex);
                            pathMax = Vector2.Max(pathMax, worldVertex);
                        }
                        
                        Vector2 pathCenter = (pathMin + pathMax) * 0.5f;
                        Vector2 pathSize = pathMax - pathMin;
                        
                        Debug.Log($"    Path {pathIndex}: center={pathCenter}, size={pathSize}");
                        Debug.Log($"    Path {pathIndex}: min={pathMin}, max={pathMax}");
                        
                        // Calculate what this should look like on minimap
                        Vector2 minimapPos = WorldToMinimapPosition(pathCenter);
                        Vector2 minimapSize = pathSize * mapScale;
                        
                        Debug.Log($"    Should appear on minimap at: pos={minimapPos}, size={minimapSize}");
                    }
                }
            }
            else
            {
                Debug.Log($"  No composite collider found");
            }
            
            Debug.Log("---");
        }
        
        if (playerController != null)
        {
            Vector3 playerWorldPos = playerController.transform.position;
            Vector2 playerMinimapPos = WorldToMinimapPosition(playerWorldPos);
            Debug.Log($"Player world position: {playerWorldPos}");
            Debug.Log($"Player minimap position: {playerMinimapPos}");
        }
        
        Debug.Log("===================================");
    } // List of terrain UI elements
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Get the RectTransform of this minimap
        minimapRect = GetComponent<RectTransform>();
        
        // Initialize the minimap
        InitializeMinimap();
    }

    // Update is called once per frame
    void Update()
    {
        if (playerController != null && playerIcon != null)
        {
            UpdatePlayerIconPosition();
        }
    }
    
    private void InitializeMinimap()
    {
        // Clear existing terrain boxes
        ClearTerrainBoxes();
        
        // Get all tilemap components from the world grid's children
        if (worldGrid != null)
        {
            worldTilemaps = worldGrid.GetComponentsInChildren<Tilemap>();
            
            if (worldTilemaps.Length == 0)
            {
                Debug.LogWarning("No tilemaps found in world grid children!");
                return;
            }
            
            // Calculate the combined bounds of all tilemaps
            CalculateWorldBounds();
            
            // Add comprehensive terrain debug
            DebugWorldTerrainLayout();
            
            // Get minimap size
            minimapSize = minimapRect.sizeDelta;
            
            // Generate terrain representation
            GenerateTerrainBoxes();
            
            Debug.Log($"Minimap initialized with {worldTilemaps.Length} tilemaps. World bounds: {worldBounds}");
        }
        else
        {
            Debug.LogError("World grid reference is missing!");
        }
    }
    
    private void CalculateWorldBounds()
    {
        if (worldTilemaps.Length == 0) return;
        
        Debug.Log("=== WORLD BOUNDS CALCULATION ===");
        
        // Start with the first tilemap's bounds
        Bounds firstTilemapLocal = worldTilemaps[0].localBounds;
        Vector3 firstWorldMin = worldTilemaps[0].transform.TransformPoint(firstTilemapLocal.min);
        Vector3 firstWorldMax = worldTilemaps[0].transform.TransformPoint(firstTilemapLocal.max);
        worldBounds = new Bounds();
        worldBounds.SetMinMax(firstWorldMin, firstWorldMax);
        
        Debug.Log($"First tilemap '{worldTilemaps[0].name}': local bounds {firstTilemapLocal}, world bounds {worldBounds}");
        
        // Expand bounds to include all other tilemaps
        for (int i = 1; i < worldTilemaps.Length; i++)
        {
            Bounds tilemapBounds = worldTilemaps[i].localBounds;
            
            // Convert local bounds to world bounds
            Vector3 worldMin = worldTilemaps[i].transform.TransformPoint(tilemapBounds.min);
            Vector3 worldMax = worldTilemaps[i].transform.TransformPoint(tilemapBounds.max);
            Bounds worldTilemapBounds = new Bounds();
            worldTilemapBounds.SetMinMax(worldMin, worldMax);
            
            Debug.Log($"Tilemap '{worldTilemaps[i].name}': local bounds {tilemapBounds}, world bounds {worldTilemapBounds}");
            
            Bounds oldWorldBounds = worldBounds;
            worldBounds.Encapsulate(worldTilemapBounds);
            Debug.Log($"World bounds after encapsulating: {oldWorldBounds} -> {worldBounds}");
        }
        
        worldSize = new Vector2(worldBounds.size.x, worldBounds.size.y);
        Debug.Log($"Final world bounds: {worldBounds}");
        Debug.Log($"World size: {worldSize}");
        Debug.Log("=================================");
    }
    
    private void UpdatePlayerIconPosition()
    {
        // Get player's world position
        Vector3 playerWorldPos = playerController.transform.position;
        
        // Convert world position to minimap position
        Vector2 minimapPosition = WorldToMinimapPosition(playerWorldPos);
        
        // Apply the position to the player icon
        playerIcon.anchoredPosition = minimapPosition;
    }
    
    private Vector2 WorldToMinimapPosition(Vector3 worldPosition)
    {
        // Normalize the world position relative to world bounds (0 to 1)
        float normalizedX = Mathf.InverseLerp(worldBounds.min.x, worldBounds.max.x, worldPosition.x);
        float normalizedY = Mathf.InverseLerp(worldBounds.min.y, worldBounds.max.y, worldPosition.y);
        
        Vector2 minimapPos;
        
        if (centerOnPlayer)
        {
            // When centering on player, calculate relative position from player
            Vector3 playerWorldPos = playerController != null ? playerController.transform.position : Vector3.zero;
            float playerNormalizedX = Mathf.InverseLerp(worldBounds.min.x, worldBounds.max.x, playerWorldPos.x);
            float playerNormalizedY = Mathf.InverseLerp(worldBounds.min.y, worldBounds.max.y, playerWorldPos.y);
            
            // Calculate offset from player position
            float relativeX = normalizedX - playerNormalizedX;
            float relativeY = normalizedY - playerNormalizedY;
            
            // Convert to minimap coordinates using map scale
            minimapPos.x = relativeX * worldSize.x * mapScale;
            minimapPos.y = relativeY * worldSize.y * mapScale;
        }
        else
        {
            // Map to minimap coordinates using map scale
            float scaledWorldWidth = worldSize.x * mapScale;
            float scaledWorldHeight = worldSize.y * mapScale;
            
            minimapPos.x = Mathf.Lerp(-scaledWorldWidth * 0.5f, scaledWorldWidth * 0.5f, normalizedX);
            minimapPos.y = Mathf.Lerp(-scaledWorldHeight * 0.5f, scaledWorldHeight * 0.5f, normalizedY);
        }
        
        // Apply manual offset
        minimapPos += mapOffset;
        
        return minimapPos;
    }
    
    // Public method to get world bounds (useful for other systems)
    public Bounds GetWorldBounds()
    {
        return worldBounds;
    }
    
    // Public method to manually refresh the minimap bounds
    public void RefreshMinimap()
    {
        InitializeMinimap();
    }
    
    private void GenerateTerrainBoxes()
    {
        foreach (Tilemap tilemap in worldTilemaps)
        {
            GenerateTerrainFromTilemapData(tilemap);
        }
    }
    
    private void GenerateTerrainFromTilemapData(Tilemap tilemap)
    {
        Debug.Log($"=== GENERATING TERRAIN FROM TILEMAP DATA: {tilemap.name} ===");
        
        // Get the bounds of tiles that actually exist in the tilemap
        BoundsInt tilemapBounds = tilemap.cellBounds;
        Debug.Log($"Tilemap cell bounds: {tilemapBounds}");
        
        if (tilemapBounds.size.x == 0 || tilemapBounds.size.y == 0 || tilemapBounds.size.z == 0)
        {
            Debug.LogWarning($"Tilemap {tilemap.name} has no tiles, skipping");
            return;
        }
        
        // Get all tiles in the tilemap
        TileBase[] allTiles = tilemap.GetTilesBlock(tilemapBounds);
        
        // Create terrain boxes for individual tiles or small rectangular groups
        CreateAccurateTerrainRepresentation(tilemap, tilemapBounds, allTiles);
        
        Debug.Log("================================================");
    }
    
    private void CreateAccurateTerrainRepresentation(Tilemap tilemap, BoundsInt bounds, TileBase[] allTiles)
    {
        // Create a 2D grid to track which tiles have been processed
        bool[,] processed = new bool[bounds.size.x, bounds.size.y];
        int terrainsCreated = 0;
        
        // Iterate through all tile positions
        for (int y = 0; y < bounds.size.y; y++)
        {
            for (int x = 0; x < bounds.size.x; x++)
            {
                if (processed[x, y]) continue;
                
                int tileIndex = x + y * bounds.size.x;
                if (tileIndex >= allTiles.Length || allTiles[tileIndex] == null) continue;
                
                // Found a tile that hasn't been processed - create a terrain box for it
                BoundsInt tileRegion = FindOptimalTileRectangle(bounds, allTiles, processed, x, y);
                
                if (tileRegion.size.x > 0 && tileRegion.size.y > 0)
                {
                    // Mark all tiles in this region as processed
                    for (int ry = 0; ry < tileRegion.size.y; ry++)
                    {
                        for (int rx = 0; rx < tileRegion.size.x; rx++)
                        {
                            int localX = tileRegion.x - bounds.x + rx;
                            int localY = tileRegion.y - bounds.y + ry;
                            if (localX >= 0 && localX < bounds.size.x && localY >= 0 && localY < bounds.size.y)
                            {
                                processed[localX, localY] = true;
                            }
                        }
                    }
                    
                    // Convert tile bounds to world bounds with precise positioning
                    Vector3 cellSize = tilemap.cellSize;
                    
                    // Calculate the exact world bounds for this tile region
                    Vector3 worldMin = tilemap.CellToWorld(tileRegion.min);
                    Vector3 worldSize = new Vector3(
                        tileRegion.size.x * cellSize.x,
                        tileRegion.size.y * cellSize.y,
                        0f
                    );
                    
                    // Create bounds using center and size to avoid floating point precision issues
                    Vector3 worldCenter = worldMin + worldSize * 0.5f;
                    
                    // Apply a small gap to prevent visual overlap
                    Vector3 adjustedWorldSize = worldSize - Vector3.one * terrainBoxGap;
                    adjustedWorldSize = Vector3.Max(adjustedWorldSize, Vector3.one * (cellSize.magnitude * 0.1f)); // Ensure minimum size
                    
                    Bounds worldRegionBounds = new Bounds(worldCenter, adjustedWorldSize);
                    
                    Debug.Log($"Tile region {terrainsCreated}: cells {tileRegion} -> world bounds {worldRegionBounds}");
                    Debug.Log($"  Cell size: {cellSize}, Region size in cells: {tileRegion.size}");
                    Debug.Log($"  World min: {worldMin}, World center: {worldCenter}");
                    Debug.Log($"  Original world size: {worldSize}, Adjusted size: {adjustedWorldSize}, Gap: {terrainBoxGap}");
                    
                    CreateTerrainBox(worldRegionBounds);
                    terrainsCreated++;
                }
            }
        }
        
        Debug.Log($"Created {terrainsCreated} terrain boxes for {tilemap.name}");
    }
    
    private BoundsInt FindOptimalTileRectangle(BoundsInt bounds, TileBase[] allTiles, bool[,] processed, int startX, int startY)
    {
        // Start with a single tile
        int minX = startX, maxX = startX;
        int minY = startY, maxY = startY;
        
        // Try to expand horizontally first (create rectangular strips)
        bool canExpandRight = true;
        while (canExpandRight && (maxX - minX + 1) < maxTileGroupSize)
        {
            int testX = maxX + 1;
            if (testX >= bounds.size.x) break;
            
            // Check if all tiles in the vertical strip at testX are valid and unprocessed
            bool stripValid = true;
            for (int y = minY; y <= maxY; y++)
            {
                if (processed[testX, y]) 
                {
                    stripValid = false;
                    break;
                }
                
                int tileIndex = testX + y * bounds.size.x;
                if (tileIndex >= allTiles.Length || allTiles[tileIndex] == null)
                {
                    stripValid = false;
                    break;
                }
            }
            
            if (stripValid)
            {
                maxX = testX;
            }
            else
            {
                canExpandRight = false;
            }
        }
        
        // Try to expand vertically (but only if it doesn't break the rectangle and doesn't exceed max size)
        bool canExpandUp = true;
        while (canExpandUp && (maxY - minY + 1) < maxTileGroupSize)
        {
            int testY = maxY + 1;
            if (testY >= bounds.size.y) break;
            
            // Check if all tiles in the horizontal strip at testY are valid and unprocessed
            bool stripValid = true;
            for (int x = minX; x <= maxX; x++)
            {
                if (processed[x, testY])
                {
                    stripValid = false;
                    break;
                }
                
                int tileIndex = x + testY * bounds.size.x;
                if (tileIndex >= allTiles.Length || allTiles[tileIndex] == null)
                {
                    stripValid = false;
                    break;
                }
            }
            
            if (stripValid)
            {
                maxY = testY;
            }
            else
            {
                canExpandUp = false;
            }
        }
        
        // Convert local coordinates back to tilemap coordinates
        Vector3Int regionMin = new Vector3Int(bounds.min.x + minX, bounds.min.y + minY, bounds.min.z);
        Vector3Int regionSize = new Vector3Int(maxX - minX + 1, maxY - minY + 1, 1);
        
        return new BoundsInt(regionMin, regionSize);
    }
    
    private void GenerateTerrainFromTilemapBounds(Tilemap tilemap)
    {
        // Simple fallback: Use tilemap bounds as a single terrain box
        Bounds tilemapBounds = tilemap.localBounds;
        
        // Convert to world bounds
        Vector3 worldMin = tilemap.transform.TransformPoint(tilemapBounds.min);
        Vector3 worldMax = tilemap.transform.TransformPoint(tilemapBounds.max);
        Bounds worldBounds = new Bounds();
        worldBounds.SetMinMax(worldMin, worldMax);
        
        Debug.Log($"Using simple tilemap bounds for {tilemap.name}: {worldBounds}");
        CreateTerrainBox(worldBounds);
    }
    
    private Bounds CalculatePathBounds(List<Vector2> pathPoints, Transform transform)
    {
        if (pathPoints.Count == 0) return new Bounds();
        
        Vector3 worldPoint = transform.TransformPoint(pathPoints[0]);
        Vector3 min = worldPoint;
        Vector3 max = worldPoint;
        
        for (int i = 1; i < pathPoints.Count; i++)
        {
            worldPoint = transform.TransformPoint(pathPoints[i]);
            min = Vector3.Min(min, worldPoint);
            max = Vector3.Max(max, worldPoint);
        }
        
        Bounds bounds = new Bounds();
        bounds.SetMinMax(min, max);
        return bounds;
    }
    
    private void CreateTerrainBox(Bounds terrainBounds)
    {
        GameObject terrainBox;
        
        if (terrainBoxPrefab != null)
        {
            terrainBox = Instantiate(terrainBoxPrefab, minimapRect);
        }
        else
        {
            terrainBox = new GameObject("TerrainBox", typeof(RectTransform), typeof(Image));
            terrainBox.transform.SetParent(minimapRect, false);
            
            Image image = terrainBox.GetComponent<Image>();
            image.color = terrainColor;
        }
        
        // Calculate terrain size and position
        Vector2 terrainSize = new Vector2(terrainBounds.size.x, terrainBounds.size.y);
        Vector2 boxSize = terrainSize * mapScale;
        
        // Ensure minimum visibility
        boxSize.x = Mathf.Max(boxSize.x, 1f);
        boxSize.y = Mathf.Max(boxSize.y, 1f);
        
        // Calculate minimap position
        Vector2 worldCenter = terrainBounds.center;
        Vector2 minimapPosition = WorldToMinimapPosition(worldCenter);
        
        // Apply to RectTransform
        RectTransform boxRect = terrainBox.GetComponent<RectTransform>();
        boxRect.anchorMin = Vector2.one * 0.5f;
        boxRect.anchorMax = Vector2.one * 0.5f;
        boxRect.pivot = Vector2.one * 0.5f;
        boxRect.sizeDelta = boxSize;
        boxRect.anchoredPosition = minimapPosition;
        
        Debug.Log($"Created terrain box: world bounds {terrainBounds} -> minimap pos {minimapPosition}, size {boxSize}");
        
        terrainBoxes.Add(terrainBox);
    }
    
    private void CreateTerrainBoxWithTilemapReference(Bounds pathBounds, Bounds tilemapBounds)
    {
        GameObject terrainBox;
        
        if (terrainBoxPrefab != null)
        {
            // Use provided prefab
            terrainBox = Instantiate(terrainBoxPrefab, minimapRect);
        }
        else
        {
            // Create a simple UI image if no prefab is provided
            terrainBox = new GameObject("TerrainBox", typeof(RectTransform), typeof(Image));
            terrainBox.transform.SetParent(minimapRect, false);
            
            Image image = terrainBox.GetComponent<Image>();
            image.color = terrainColor;
        }
        
        Debug.Log("=== TERRAIN BOX CREATION DEBUG ===");
        Debug.Log($"Path bounds: center={pathBounds.center}, size={pathBounds.size}");
        Debug.Log($"World bounds: center={worldBounds.center}, size={worldBounds.size}");
        Debug.Log($"Map scale: {mapScale}");
        Debug.Log($"Center on player: {centerOnPlayer}");
        Debug.Log($"Minimap size: {minimapSize}");
        
        // Calculate box size directly from path size
        Vector2 pathSize = new Vector2(pathBounds.size.x, pathBounds.size.y);
        Vector2 boxSize = pathSize * mapScale;
        
        Debug.Log($"Raw path size: {pathSize}");
        Debug.Log($"Scaled box size (before min): {boxSize}");
        
        // Apply minimum size for visibility
        float minVisibleSize = 0.5f;
        boxSize.x = Mathf.Max(boxSize.x, minVisibleSize);
        boxSize.y = Mathf.Max(boxSize.y, minVisibleSize);
        
        Debug.Log($"Final box size (after min): {boxSize}");
        
        // Calculate center position
        Vector2 worldCenter = pathBounds.center;
        Vector2 boxCenter = WorldToMinimapPosition(worldCenter);
        
        Debug.Log($"World center: {worldCenter}");
        Debug.Log($"Calculated minimap center: {boxCenter}");
        
        // Let's also calculate what the position SHOULD be manually for comparison
        // Normalize the world position relative to world bounds (0 to 1)
        float normalizedX = Mathf.InverseLerp(worldBounds.min.x, worldBounds.max.x, worldCenter.x);
        float normalizedY = Mathf.InverseLerp(worldBounds.min.y, worldBounds.max.y, worldCenter.y);
        
        Debug.Log($"Normalized position: ({normalizedX}, {normalizedY})");
        
        Vector2 expectedMinimapPos;
        if (centerOnPlayer && playerController != null)
        {
            Vector3 playerWorldPos = playerController.transform.position;
            float playerNormalizedX = Mathf.InverseLerp(worldBounds.min.x, worldBounds.max.x, playerWorldPos.x);
            float playerNormalizedY = Mathf.InverseLerp(worldBounds.min.y, worldBounds.max.y, playerWorldPos.y);
            
            float relativeX = normalizedX - playerNormalizedX;
            float relativeY = normalizedY - playerNormalizedY;
            
            expectedMinimapPos.x = relativeX * worldSize.x * mapScale;
            expectedMinimapPos.y = relativeY * worldSize.y * mapScale;
            
            Debug.Log($"Player world pos: {playerWorldPos}");
            Debug.Log($"Player normalized: ({playerNormalizedX}, {playerNormalizedY})");
            Debug.Log($"Relative to player: ({relativeX}, {relativeY})");
        }
        else
        {
            float scaledWorldWidth = worldSize.x * mapScale;
            float scaledWorldHeight = worldSize.y * mapScale;
            
            expectedMinimapPos.x = Mathf.Lerp(-scaledWorldWidth * 0.5f, scaledWorldWidth * 0.5f, normalizedX);
            expectedMinimapPos.y = Mathf.Lerp(-scaledWorldHeight * 0.5f, scaledWorldHeight * 0.5f, normalizedY);
            
            Debug.Log($"Scaled world size: ({scaledWorldWidth}, {scaledWorldHeight})");
            Debug.Log($"Lerp range X: [{-scaledWorldWidth * 0.5f}, {scaledWorldWidth * 0.5f}]");
            Debug.Log($"Lerp range Y: [{-scaledWorldHeight * 0.5f}, {scaledWorldHeight * 0.5f}]");
        }
        
        expectedMinimapPos += mapOffset;
        Debug.Log($"Expected minimap position (after offset): {expectedMinimapPos}");
        Debug.Log($"Position matches WorldToMinimapPosition? {Vector2.Distance(boxCenter, expectedMinimapPos) < 0.001f}");
        
        // Apply to RectTransform
        RectTransform boxRect = terrainBox.GetComponent<RectTransform>();
        
        // Set anchors to center to ensure consistent sizing behavior
        boxRect.anchorMin = Vector2.one * 0.5f;
        boxRect.anchorMax = Vector2.one * 0.5f;
        boxRect.pivot = Vector2.one * 0.5f;
        
        boxRect.sizeDelta = boxSize;
        boxRect.anchoredPosition = boxCenter;
        
        // Check if the terrain box is within minimap bounds
        bool withinBoundsX = Mathf.Abs(boxCenter.x) <= minimapSize.x * 0.5f;
        bool withinBoundsY = Mathf.Abs(boxCenter.y) <= minimapSize.y * 0.5f;
        
        Debug.Log($"Terrain box within minimap bounds? X: {withinBoundsX}, Y: {withinBoundsY}");
        Debug.Log($"Distance from minimap center: {Vector2.Distance(boxCenter, Vector2.zero)}");
        Debug.Log($"Minimap radius (approx): {Mathf.Min(minimapSize.x, minimapSize.y) * 0.5f}");
        Debug.Log("=====================================");
        
        // Add to our list for cleanup
        terrainBoxes.Add(terrainBox);
    }
    
    private void CreateTerrainBox(Bounds terrainBounds, float scaleFactorX, float scaleFactorY)
    {
        GameObject terrainBox;
        
        if (terrainBoxPrefab != null)
        {
            // Use provided prefab
            terrainBox = Instantiate(terrainBoxPrefab, minimapRect);
        }
        else
        {
            // Create a simple UI image if no prefab is provided
            terrainBox = new GameObject("TerrainBox", typeof(RectTransform), typeof(Image));
            terrainBox.transform.SetParent(minimapRect, false);
            
            Image image = terrainBox.GetComponent<Image>();
            image.color = terrainColor;
        }
        
        // Scale factors represent terrain cell density - use them to normalize terrain representation
        // For a tilemap with smaller cells, we want smaller representation per unit area
        // For disconnected paths, this ensures they all have the same visual density as the parent tilemap
        Vector2 terrainSize = new Vector2(terrainBounds.size.x, terrainBounds.size.y);
        Vector2 boxSize = terrainSize * mapScale;
        
        // Apply density scaling - this affects how "thick" the terrain appears on the minimap
        // Smaller scale factors = denser terrain = thinner representation
        boxSize.x *= scaleFactorX;
        boxSize.y *= scaleFactorY;
        
        // Calculate center position using the existing method
        Vector2 worldCenter = terrainBounds.center;
        Vector2 boxCenter = WorldToMinimapPosition(worldCenter);
        
        // Debug information - comparing direct calculation vs coordinate conversion
        Vector2 minimapMin = WorldToMinimapPosition(terrainBounds.min);
        Vector2 minimapMax = WorldToMinimapPosition(terrainBounds.max);
        Vector2 coordinateBasedSize = new Vector2(
            Mathf.Abs(minimapMax.x - minimapMin.x),
            Mathf.Abs(minimapMax.y - minimapMin.y)
        );
        
        // Ensure minimum size so boxes are visible
        boxSize.x = Mathf.Max(boxSize.x, 1f);
        boxSize.y = Mathf.Max(boxSize.y, 1f);
        
        // Apply to RectTransform
        RectTransform boxRect = terrainBox.GetComponent<RectTransform>();
        
        // Set anchors to center to ensure consistent sizing behavior
        boxRect.anchorMin = Vector2.one * 0.5f;
        boxRect.anchorMax = Vector2.one * 0.5f;
        boxRect.pivot = Vector2.one * 0.5f;
        
        boxRect.sizeDelta = boxSize;
        boxRect.anchoredPosition = boxCenter;
        
        // Debug: Check what Unity actually set
        Debug.Log($"Set sizeDelta to: {boxSize}");
        Debug.Log($"Actual sizeDelta is: {boxRect.sizeDelta}");
        Debug.Log($"Actual rect size is: {boxRect.rect.size}");
        Debug.Log($"Minimap container sizeDelta: {minimapRect.sizeDelta}");
        Debug.Log($"Canvas scale factor: {GetComponentInParent<Canvas>()?.scaleFactor ?? 1f}");
        Debug.Log($"Terrain box position: {boxRect.anchoredPosition}");
        Debug.Log($"Is terrain box visible in minimap bounds? X: {Mathf.Abs(boxRect.anchoredPosition.x) <= minimapRect.sizeDelta.x * 0.5f}, Y: {Mathf.Abs(boxRect.anchoredPosition.y) <= minimapRect.sizeDelta.y * 0.5f}");
        Debug.Log("---");
        
        // Add to our list for cleanup
        terrainBoxes.Add(terrainBox);
    }
    
    private void ClearTerrainBoxes()
    {
        foreach (GameObject box in terrainBoxes)
        {
            if (box != null)
            {
                DestroyImmediate(box);
            }
        }
        terrainBoxes.Clear();
    }
    
    // Debug method to visualize world bounds in scene view
    private void OnDrawGizmosSelected()
    {
        if (worldBounds.size != Vector3.zero)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(worldBounds.center, worldBounds.size);
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(worldBounds.min, 0.5f);
            Gizmos.DrawWireSphere(worldBounds.max, 0.5f);
        }
    }
}
