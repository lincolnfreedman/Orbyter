using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class Minimap : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController_pif playerController;
    [SerializeField] private GameObject worldGrid;
    [SerializeField] private RectTransform playerIcon;
    
    [Header("Minimap Settings")]
    [SerializeField] private float mapScale = 0.1f;
    [SerializeField] private bool centerOnPlayer = false;
    [SerializeField] private Vector2 mapOffset = Vector2.zero;
    [SerializeField] private Color terrainColor = Color.gray;
    [SerializeField] private GameObject terrainBoxPrefab;
    [SerializeField] private int maxTileGroupSize = 8;
    [SerializeField] private float terrainBoxGap = 0.05f;
    
    [Header("Display Range")]
    [SerializeField] private bool limitDisplayRange = true;
    [SerializeField] private float maxDisplayRangeX = 50f; // Maximum horizontal distance from player to show terrain
    [SerializeField] private float maxDisplayRangeY = 30f; // Maximum vertical distance from player to show terrain
    
    private RectTransform minimapRect;
    private Tilemap[] worldTilemaps;
    private Bounds worldBounds;
    private Vector2 worldSize;
    private Vector2 minimapSize;
    private List<GameObject> terrainBoxes = new List<GameObject>();
    private List<Bounds> terrainBounds = new List<Bounds>(); // Store terrain bounds for position updates
    
    void Start()
    {
        minimapRect = GetComponent<RectTransform>();
        InitializeMinimap();
    }

    void Update()
    {
        if (playerController != null && playerIcon != null)
        {
            UpdatePlayerIconPosition();
            
            // If centering on player, update terrain positions as player moves
            if (centerOnPlayer)
            {
                UpdateTerrainPositions();
            }
            else if (limitDisplayRange)
            {
                // Update terrain visibility when not centering on player
                UpdateTerrainVisibility();
            }
        }
    }
    
    private void InitializeMinimap()
    {
        ClearTerrainBoxes();
        
        if (worldGrid != null)
        {
            worldTilemaps = worldGrid.GetComponentsInChildren<Tilemap>();
            
            if (worldTilemaps.Length == 0)
            {
                Debug.LogWarning("No tilemaps found in world grid children!");
                return;
            }
            
            CalculateWorldBounds();
            minimapSize = minimapRect.sizeDelta;
            GenerateTerrainBoxes();
            
            // Ensure player icon appears on top of terrain boxes
            if (playerIcon != null)
            {
                playerIcon.SetAsLastSibling();
            }
        }
        else
        {
            Debug.LogError("World grid reference is missing!");
        }
    }
    
    private void CalculateWorldBounds()
    {
        if (worldTilemaps.Length == 0) return;
        
        // Start with the first tilemap's bounds
        Bounds firstTilemapLocal = worldTilemaps[0].localBounds;
        Vector3 firstWorldMin = worldTilemaps[0].transform.TransformPoint(firstTilemapLocal.min);
        Vector3 firstWorldMax = worldTilemaps[0].transform.TransformPoint(firstTilemapLocal.max);
        worldBounds = new Bounds();
        worldBounds.SetMinMax(firstWorldMin, firstWorldMax);
        
        // Expand bounds to include all other tilemaps
        for (int i = 1; i < worldTilemaps.Length; i++)
        {
            Bounds tilemapBounds = worldTilemaps[i].localBounds;
            Vector3 worldMin = worldTilemaps[i].transform.TransformPoint(tilemapBounds.min);
            Vector3 worldMax = worldTilemaps[i].transform.TransformPoint(tilemapBounds.max);
            Bounds worldTilemapBounds = new Bounds();
            worldTilemapBounds.SetMinMax(worldMin, worldMax);
            worldBounds.Encapsulate(worldTilemapBounds);
        }
        
        worldSize = new Vector2(worldBounds.size.x, worldBounds.size.y);
    }
    
    private void UpdatePlayerIconPosition()
    {
        Vector3 playerWorldPos = playerController.transform.position;
        Vector2 minimapPosition;
        
        if (centerOnPlayer)
        {
            // When centering on player, the player icon stays at the center
            minimapPosition = mapOffset;
        }
        else
        {
            // When not centering, calculate the player's position on the minimap
            minimapPosition = WorldToMinimapPosition(playerWorldPos);
        }
        
        playerIcon.anchoredPosition = minimapPosition;
    }
    
    private Vector2 WorldToMinimapPosition(Vector3 worldPosition)
    {
        float normalizedX = Mathf.InverseLerp(worldBounds.min.x, worldBounds.max.x, worldPosition.x);
        float normalizedY = Mathf.InverseLerp(worldBounds.min.y, worldBounds.max.y, worldPosition.y);
        
        Vector2 minimapPos;
        
        if (centerOnPlayer && playerController != null)
        {
            // When centering on player, calculate position relative to player
            Vector3 playerWorldPos = playerController.transform.position;
            float playerNormalizedX = Mathf.InverseLerp(worldBounds.min.x, worldBounds.max.x, playerWorldPos.x);
            float playerNormalizedY = Mathf.InverseLerp(worldBounds.min.y, worldBounds.max.y, playerWorldPos.y);
            
            minimapPos.x = (normalizedX - playerNormalizedX) * worldSize.x * mapScale;
            minimapPos.y = (normalizedY - playerNormalizedY) * worldSize.y * mapScale;
        }
        else
        {
            // Fixed minimap - map world position to minimap coordinates
            float scaledWorldWidth = worldSize.x * mapScale;
            float scaledWorldHeight = worldSize.y * mapScale;
            
            minimapPos.x = Mathf.Lerp(-scaledWorldWidth * 0.5f, scaledWorldWidth * 0.5f, normalizedX);
            minimapPos.y = Mathf.Lerp(-scaledWorldHeight * 0.5f, scaledWorldHeight * 0.5f, normalizedY);
        }
        
        return minimapPos + mapOffset;
    }
    
    public Bounds GetWorldBounds()
    {
        return worldBounds;
    }
    
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
        BoundsInt tilemapBounds = tilemap.cellBounds;
        
        if (tilemapBounds.size.x == 0 || tilemapBounds.size.y == 0 || tilemapBounds.size.z == 0)
        {
            return;
        }
        
        TileBase[] allTiles = tilemap.GetTilesBlock(tilemapBounds);
        CreateAccurateTerrainRepresentation(tilemap, tilemapBounds, allTiles);
    }
    
    private void CreateAccurateTerrainRepresentation(Tilemap tilemap, BoundsInt bounds, TileBase[] allTiles)
    {
        bool[,] processed = new bool[bounds.size.x, bounds.size.y];
        
        for (int y = 0; y < bounds.size.y; y++)
        {
            for (int x = 0; x < bounds.size.x; x++)
            {
                if (processed[x, y]) continue;
                
                int tileIndex = x + y * bounds.size.x;
                if (tileIndex >= allTiles.Length || allTiles[tileIndex] == null) continue;
                
                BoundsInt tileRegion = FindOptimalTileRectangle(bounds, allTiles, processed, x, y);
                
                if (tileRegion.size.x > 0 && tileRegion.size.y > 0)
                {
                    // Mark tiles as processed
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
                    
                    // Convert to world bounds and create terrain box
                    Vector3 cellSize = tilemap.cellSize;
                    Vector3 worldMin = tilemap.CellToWorld(tileRegion.min);
                    Vector3 worldSize = new Vector3(
                        tileRegion.size.x * cellSize.x,
                        tileRegion.size.y * cellSize.y,
                        0f
                    );
                    
                    Vector3 worldCenter = worldMin + worldSize * 0.5f;
                    Vector3 adjustedWorldSize = worldSize - Vector3.one * terrainBoxGap;
                    adjustedWorldSize = Vector3.Max(adjustedWorldSize, Vector3.one * (cellSize.magnitude * 0.1f));
                    
                    Bounds worldRegionBounds = new Bounds(worldCenter, adjustedWorldSize);
                    CreateTerrainBox(worldRegionBounds);
                }
            }
        }
    }
    
    private BoundsInt FindOptimalTileRectangle(BoundsInt bounds, TileBase[] allTiles, bool[,] processed, int startX, int startY)
    {
        int minX = startX, maxX = startX;
        int minY = startY, maxY = startY;
        
        // Expand horizontally
        while (maxX - minX + 1 < maxTileGroupSize && CanExpandHorizontally(bounds, allTiles, processed, minY, maxY, maxX + 1))
        {
            maxX++;
        }
        
        // Expand vertically
        while (maxY - minY + 1 < maxTileGroupSize && CanExpandVertically(bounds, allTiles, processed, minX, maxX, maxY + 1))
        {
            maxY++;
        }
        
        Vector3Int regionMin = new Vector3Int(bounds.min.x + minX, bounds.min.y + minY, bounds.min.z);
        Vector3Int regionSize = new Vector3Int(maxX - minX + 1, maxY - minY + 1, 1);
        
        return new BoundsInt(regionMin, regionSize);
    }
    
    private bool CanExpandHorizontally(BoundsInt bounds, TileBase[] allTiles, bool[,] processed, int minY, int maxY, int testX)
    {
        if (testX >= bounds.size.x) return false;
        
        for (int y = minY; y <= maxY; y++)
        {
            if (processed[testX, y] || !IsTileValid(bounds, allTiles, testX, y))
                return false;
        }
        return true;
    }
    
    private bool CanExpandVertically(BoundsInt bounds, TileBase[] allTiles, bool[,] processed, int minX, int maxX, int testY)
    {
        if (testY >= bounds.size.y) return false;
        
        for (int x = minX; x <= maxX; x++)
        {
            if (processed[x, testY] || !IsTileValid(bounds, allTiles, x, testY))
                return false;
        }
        return true;
    }
    
    private bool IsTileValid(BoundsInt bounds, TileBase[] allTiles, int x, int y)
    {
        int tileIndex = x + y * bounds.size.x;
        return tileIndex < allTiles.Length && allTiles[tileIndex] != null;
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
            
            // Set transparent color (alpha 200/255 ≈ 0.784)
            Image terrainImage = terrainBox.GetComponent<Image>();
            Color transparentColor = terrainColor;
            transparentColor.a = 200f / 255f;
            terrainImage.color = transparentColor;
        }
        
        Vector2 boxSize = new Vector2(terrainBounds.size.x, terrainBounds.size.y) * mapScale;
        boxSize.x = Mathf.Max(boxSize.x, 1f);
        boxSize.y = Mathf.Max(boxSize.y, 1f);
        
        Vector2 minimapPosition = WorldToMinimapPosition(terrainBounds.center);
        
        RectTransform boxRect = terrainBox.GetComponent<RectTransform>();
        boxRect.anchorMin = Vector2.one * 0.5f;
        boxRect.anchorMax = Vector2.one * 0.5f;
        boxRect.pivot = Vector2.one * 0.5f;
        boxRect.sizeDelta = boxSize;
        boxRect.anchoredPosition = minimapPosition;
        
        // Ensure terrain boxes appear behind the player icon
        terrainBox.transform.SetAsFirstSibling();
        
        // Set initial visibility based on display range
        bool shouldBeVisible = !limitDisplayRange || IsTerrainWithinDisplayRange(terrainBounds);
        terrainBox.SetActive(shouldBeVisible);
        
        terrainBoxes.Add(terrainBox);
        this.terrainBounds.Add(terrainBounds); // Store bounds for position updates
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
        terrainBounds.Clear();
    }
    
    private void UpdateTerrainPositions()
    {
        // Update terrain box positions when centering on player
        for (int i = 0; i < terrainBoxes.Count && i < terrainBounds.Count; i++)
        {
            if (terrainBoxes[i] != null)
            {
                // Check if terrain should be visible based on display range
                bool shouldBeVisible = !limitDisplayRange || IsTerrainWithinDisplayRange(terrainBounds[i]);
                terrainBoxes[i].SetActive(shouldBeVisible);
                
                if (shouldBeVisible)
                {
                    Vector2 newPosition = WorldToMinimapPosition(terrainBounds[i].center);
                    terrainBoxes[i].GetComponent<RectTransform>().anchoredPosition = newPosition;
                }
            }
        }
    }
    
    private void UpdateTerrainVisibility()
    {
        // Update terrain box visibility when not centering on player
        for (int i = 0; i < terrainBoxes.Count && i < terrainBounds.Count; i++)
        {
            if (terrainBoxes[i] != null)
            {
                bool shouldBeVisible = !limitDisplayRange || IsTerrainWithinDisplayRange(terrainBounds[i]);
                terrainBoxes[i].SetActive(shouldBeVisible);
            }
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        if (worldBounds.size != Vector3.zero)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(worldBounds.center, worldBounds.size);
        }
    }

    private bool IsTerrainWithinDisplayRange(Bounds terrainBounds)
    {
        if (playerController == null) return true; // Show all terrain if no player reference
        
        Vector3 playerWorldPos = playerController.transform.position;
        Vector3 terrainCenter = terrainBounds.center;
        
        // Calculate distance from player to terrain center
        float distanceX = Mathf.Abs(terrainCenter.x - playerWorldPos.x);
        float distanceY = Mathf.Abs(terrainCenter.y - playerWorldPos.y);
        
        // Check if terrain is within display range
        return distanceX <= maxDisplayRangeX && distanceY <= maxDisplayRangeY;
    }
}
