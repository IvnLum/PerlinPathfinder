using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class PathNode
{
    public Vector2 location;
    public float G, H, F;
    public GameObject marker;
    public PathNode parent;
    public PathNode(Vector2 loc, float G, float H, float F, GameObject marker, PathNode p)
    {
        this.location = loc;
        this.G = G;
        this.H = H;
        this.F = F;
        this.marker = marker;
        parent = p;
    }
}

public class APathFind : MonoBehaviour
{
    public int pathId = 1;
    public PathNode start, end;
    public List<PathNode> path;
    public List<PathNode> closed;
    public PerlinMap pm;
    public GameObject pointA, pointB, pointPath;
    public List<GameObject> points;

    private List<Vector2> directions;
    private bool found = false;
    private bool noPath = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        directions = new List<Vector2>
        {
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(-1, 0),
            new Vector2(0, -1)
        };
        Restart();
    }

    // Update is called once per frame
    void PathSearch()
    {
        if (start == null || end == null || found || noPath)
            return;
        if (pm.TileRegionVec2(start.location) != pm.TileRegionVec2(end.location))
        {
            noPath = true;
            return;
        }
        PathNode current = start;
        if (closed.Count != 0)
            current = closed[closed.Count-1];
        foreach (Vector2 dir in directions)
        {
            Vector2 next = current.location + dir;
            
            if (next.x < 0 || next.x >= pm.width || next.y < 0 || next.y >= pm.height) continue;
            if (pm.TileValue((int)next.x, (int)next.y) != pathId) continue;
            if (IsClosed(next)) continue;
            float G = Vector2.Distance(current.location, next) + current.G;
            float H = Vector2.Distance(next, end.location);
            float F = G + H;
            if (H == 0)
                found = true;

            if (!UpdatePathNode(next, G, H, F, current))
            {
                path.Add(new PathNode(next, G, H, F, null, current));
            }
        }
        path = path.OrderBy(o => o.F).ToList();
        closed.Add(path.ElementAt(0));
        path.RemoveAt(0);

        if (!found)return;

        while(current != null)
        {
            GameObject go = Instantiate(pointPath, transform);
            go.transform.localPosition = new Vector3(current.location.x, current.location.y, -0.9f);
            points.Add(go);
            current = current.parent;
     
        }
    }

    bool UpdatePathNode(Vector2 loc, float G, float H, float F, PathNode parent)
    {
        foreach (PathNode p in path)
        {
            if (p.location.Equals(loc))
            {
                p.G = G;
                p.H = H;
                p.F = F;
                p.parent = parent;
                return true;
            }
        }
        return false;
    }
    bool IsClosed(Vector2 loc)
    {
        foreach (PathNode l in closed)
        {
            if (l.Equals(loc)) return true;
        }
        return false;
    }
    void Restart()
    {
        noPath = false;
        found = false;
        if (start != null) 
        {
            Destroy(start.marker);
            start = null;
        }
        if (end != null)
        {
            Destroy(end.marker);
            end = null;
        }
        foreach(GameObject go in points)
        {
            Destroy(go);
        }

        path = new List<PathNode>();
        closed = new List<PathNode>();
        points = new List<GameObject>();
    }
    
    void setStartEnd(Vector2 loc)
    {
        if (pm.TileValue((int)loc.x, (int)loc.y) != pathId)
            return;
        PathNode p = new PathNode(loc, 0, 0, 0, null, null);
        GameObject point;
        if (start == null)
        {
            point = Instantiate(pointA, transform);
            start = p;
        }
        else
        {
            point = Instantiate(pointB, transform);
            end = p;
        }
        p.marker = point;
        point.transform.localPosition = new Vector3(loc.x, loc.y, -1.0f);
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (start != null && end != null)
            {
                return;
            }
            RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
            if(hit.collider != null)
            {
                Vector2 point = hit.point - (Vector2)transform.position;
                setStartEnd(new Vector2(Mathf.Round(point.x), Mathf.Round(point.y)));
            }
        }
        if (Input.GetMouseButtonDown(1))
        {
            if (start != null && end != null)
            {
                return;
            }

            setStartEnd(new Vector2(Mathf.Round(UnityEngine.Random.Range(0, pm.width)), Mathf.Round(UnityEngine.Random.Range(0, pm.height))));
        }
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("Pathfind key pressed (P)");
            while (!found && !noPath)
                PathSearch();
            if (noPath)
                Debug.Log("No posible path!");
            else 
                Debug.Log("Path found!");
        }
        
        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log("Restart nodes key pressed (T)");
            Restart();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("Regenerate map key pressed (R)");
            Restart();
            pm.RandomizeOffset();
            pm.CleanMapTiles();
            pm.GenerateMap();
            pm.ClasifyRegions();
        }
    }
}
