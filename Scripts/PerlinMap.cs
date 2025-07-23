using System.Collections.Generic;
using UnityEngine;

public class PerlinMap : MonoBehaviour
{
    public List<GameObject> tiles;
    private List<List<GameObject>> perlin_tiles;
    private Dictionary<GameObject, int> tiles_value;
    private Dictionary<GameObject, int> tiles_region;
    public int x_offset = 0, y_offset = 0;
    public int width, height;
    public float magnification = 1.0f;
    private int tile_count = 0;

    void Awake()
    {
        tile_count = tiles.Count;
    }
    void Start()
    {
        RandomizeOffset();
        GenerateMap();
    }
    public void RandomizeOffset()
    {
        x_offset = UnityEngine.Random.Range(0, 255);
        y_offset = UnityEngine.Random.Range(0, 255);
    }
    public void CleanMapTiles()
    {
        foreach(List<GameObject> pl in perlin_tiles)
        {
            foreach(GameObject go in pl)
            {
                Destroy(go);
            }
        }
    }

    public void GenerateMap()
    {
        perlin_tiles = new List<List<GameObject>>();
        tiles_value = new Dictionary<GameObject, int>();
        tiles_region = new Dictionary<GameObject, int>();
        for (int y = 0; y < height; y++)
        {
            perlin_tiles.Add(new List<GameObject>());
            for (int x = 0; x < width; x++)
            {
                int idx = GetPerlinIdx(x, y, tile_count);
                GameObject current = GetTile(idx, x, y);
                tiles_value[current] = idx;
                tiles_region[current] = -1;
                perlin_tiles[y].Add(current);
            }
        }
    }

    /* private int DynamicPerlinRegionSplit(bool[,] visited, int x0, int y0, int region)
     *
     * To optimize APathFind the best option is to split by regions the perlin created map with a given value
     * If selected tiles belong to different regions the algorithm will evaluate this
     * and prune returning early (No posible path)
     * (Recursive algorithm ported to iterative by using an arguments tuple stack)
     */

    private int DynamicPerlinRegionSplit(bool[,] visited, int x0, int y0, int region)
    {
        int x = x0, y = y0;
        int sibling = TileValue(x0, y0);
        int count = 1;
        visited[y, x] = true;
        List<Vector2> directions = new List<Vector2> {
            new Vector2(-1, 0),new Vector2(0, -1),new Vector2(1, 0),new Vector2(0, 1)
        };
        GameObject current;

        Stack<(GameObject, (int, int))> recursionStack = new Stack<(GameObject, (int, int))>();
        recursionStack.Push((perlin_tiles[y][x], (x, y)));
        while (recursionStack.Count > 0)
        {
        // Recursive begin (now stack pop)
        (current, (x, y)) = recursionStack.Pop();

        tiles_region[current] = region;
        foreach(Vector2 dir in directions)
        {
            int x_dyn = x+(int)dir.x;
            int y_dyn = y+(int)dir.y;
            if (TileValue(x_dyn, y_dyn) == sibling && !visited[y_dyn, x_dyn])
            {
                // recursive call (now stack push)
                recursionStack.Push((perlin_tiles[y_dyn][x_dyn], (x_dyn, y_dyn)));
                visited[y_dyn, x_dyn] = true;
                count++;
            }
        }

        }
        return count;
    }
    public void ClasifyRegions()
    {
        if (tiles_region == null)
            return;
        bool[,] visited = new bool[height, width];
        int regionCount = 0;
        int visitedCount = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (visited[y, x])
                    continue;
                visitedCount += DynamicPerlinRegionSplit(visited, x, y, regionCount++);
                if (visitedCount == tiles.Count) return;
            }
        }
    }
    int GetPerlinIdx(int x, int y, int count)
    {
        float perlin = Mathf.PerlinNoise(
            (x - x_offset) / magnification,
            (y - y_offset) / magnification
        );
        float scaled_perlin = count * Mathf.Clamp(perlin, 0.0f, 1.0f - 1e-5f);
        return Mathf.FloorToInt(scaled_perlin);
    }
    GameObject GetTile(int idx, int x, int y)
    {
        GameObject t = Instantiate(tiles[idx], transform);
        t.transform.localPosition = new Vector3(x, y, 0.5f);
        return t;
    }
    public int TileValue(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
            return -1;
        return tiles_value[perlin_tiles[y][x]];
    }
    public int TileRegion(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
            return -1;
        return tiles_region[perlin_tiles[y][x]];
    }
    public int TileValueVec2(Vector2 loc)
    {
        return TileValue((int)loc.x, (int)loc.y);
    }
    public int TileRegionVec2(Vector2 loc)
    {
        return TileRegion((int)loc.x, (int)loc.y);
    }
}
