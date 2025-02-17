﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct BuildingCell
{
    public BuildingCell(Vector3 pos, float size, Vector3 offSetDir, bool isLeft, Quaternion rot, int id)
    {
        this.pos = pos;
        //this.min = min;
        //this.max = max;
        //this.minB = new Vector2(min.x)
        this.radius = size;
        this.offSetDir = offSetDir;
        this.isLeft = isLeft;
        this.rot = rot;
        this.id = id;
    }

    public Vector3 pos;
    public Vector3 offSetDir;
    //private Vector2 min;

    //private Vector2 minB;
    //private Vector2 max;
    //private Vector2 maxB;
    public bool isLeft;

    public Quaternion rot;
    public float radius;
    public int id;
}

[System.Serializable]
public struct MinMax
{
    public float min;
    public float max;

    public float Get()
    {
        return Random.Range(min, max);
    }
}

public class CellGenerator : Generator
{
    private List<BuildingCell> cells = new List<BuildingCell>();

    [SerializeField]
    private CityGenerator cityGenerator;

    //[SerializeField]
    //private float minOffset;

    //[SerializeField]
    //private float maxOffset;
    [SerializeField]
    private MinMax buffer;

    //[SerializeField]
    //private MinMax cellRadius;

    //[SerializeField]
    //private MinMax width;

    [SerializeField]
    private float initialSpace;

    [SerializeField]
    private RoadGenerator roadGenerator;

    [SerializeField]
    private TowerGenerator towerGenerator;

    [SerializeField]
    private PlayerSpawnGenerator playerSpawnGenerator;

    [Min(0)]
    [SerializeField]
    private float playerBuffer;

    [SerializeField]
    private float towerBuffer;

    public List<BuildingCell> GetCells()
    {
        return cells;
    }

    public override void Clear()
    {
        cells.Clear();
    }

    public override void Generate()
    {
        Debug.Log("Begin cell generation.");
        Clear();
        foreach (RoadGenerator.RoadPath path in roadGenerator.GetRoadOuterPaths())
        {
            int id = path.GetHashCode() +Random.Range(-int.MaxValue, int.MaxValue);

            {
                GameObject building = cityGenerator.city.SelectMesh();
                if (!building)
                {
                    Debug.LogError("Building is null: " + gameObject.name);
                    break;
                }
                if (!building.GetComponent<ProceduralBuilding>())
                {
                    Debug.LogError("Building is not a PB: " + building.name);
                    break;
                }
                float currCellRadius = building.GetComponent<ProceduralBuilding>().GetRadius();
                // add building at end of road
                cells.Add(new BuildingCell(path.end, currCellRadius, path.dir, false, Quaternion.identity, id));
                //

            }
            Vector3 dir = path.dir;
            Vector3 pDir = Vector3.Cross(dir, Vector3.up).normalized;
            Quaternion rot = Quaternion.LookRotation(pDir, Vector3.up);
            for (int i = 0; i < 2; ++i)
            {
                bool isLeft = true;
                // initial path values
                float currDist = 0;
                if (i == 1)
                {
                    rot = Quaternion.LookRotation(-pDir, Vector3.up);
                    pDir = -pDir;
                    isLeft = false;
                }
                Vector3 startPos = path.start + pDir * path.width / 2;
                Vector3 currPos = startPos;
                currPos += dir * initialSpace;
                currPos += dir * buffer.Get();
                while (currDist < path.Length())    // main cell loop
                {
                    // float currCellRadius = cellRadius.Get();
                    GameObject building = cityGenerator.city.SelectMesh();
                    if (!building)
                    {
                        Debug.LogError("Building is null: " + gameObject.name);
                        break;
                    }
                    if (!building.GetComponent<ProceduralBuilding>())
                    {
                        Debug.LogError("Building is not a PB: " + building.name);
                        break;
                    }
                    float currCellRadius = building.GetComponent<ProceduralBuilding>().GetRadius();
                    //float cellWidth = width.Get();
                    float spacing = buffer.Get();
                    currDist += currCellRadius;
                    currPos = currDist * dir + startPos;
                    //Vector3 end = currPos + dir * cellLength;
                    //Vector3 cellMax = end + pDir * cellWidth;

                    bool isAvailable = true;
                    // check for road
                    foreach (RoadGenerator.RoadPath road in roadGenerator.roadSubPaths)
                    {
                        if (Vector3.Distance((road.start + road.end) / 2, currPos) < currCellRadius)
                        {
                            isAvailable = false;
                            break;
                        }
                    }
                    // check for other cell
                    foreach (BuildingCell cell in cells)
                    {
                        if (cell.id == id)
                            continue;
                        if (Vector3.Distance(cell.pos, currPos) < currCellRadius + cell.radius*1.4f)
                        {
                            isAvailable = false;
                            break;
                        }
                    }
                    // check for tower
                    foreach (PoissonPoint point in towerGenerator.GetPoisson().GetPoints())
                    {
                        if (Vector3.Distance(currPos, point.pos) < towerBuffer + currCellRadius)
                        {
                            isAvailable = false;
                            break;
                        }
                    }
                    // check for player
                    foreach (Vector3 point in PlayerSpawnGenerator.playerSpawnPos)
                    {
                        if (Vector3.Distance(currPos, point) < playerBuffer + currCellRadius)
                        {
                            isAvailable = false;
                            break;
                        }
                    }
                    if (Vector3.Distance(currPos, PlayerSpawnGenerator.hunterSpawnPos) < playerBuffer + currCellRadius)
                    {
                        isAvailable = false;
                    }
                    // check for hunter
                    if (isAvailable)
                    {
                        // create cell
                        cells.Add(new BuildingCell(currPos, currCellRadius, pDir, isLeft, rot, id));
                    }
                    currDist += spacing + currCellRadius;
                }
            }
        }
        Debug.Log("Generated Cells.");
    }

    private void OnDrawGizmos()
    {
        if (!gizmosEnabled)
            return;
        foreach (BuildingCell cell in cells)
        {
            if (cell.isLeft)
                Gizmos.color = Color.blue;
            else
                Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(cell.pos, cell.radius);
        }
        // tower
        Gizmos.color = Color.red;
        foreach (PoissonPoint point in towerGenerator.GetPoisson().GetPoints())
        {
            Gizmos.DrawWireSphere(point.pos, towerBuffer);
        }
        Gizmos.color = Color.magenta;
        foreach (Vector3 point in PlayerSpawnGenerator.playerSpawnPos)
        {
            Gizmos.DrawWireSphere(point, playerBuffer);
        }
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(PlayerSpawnGenerator.hunterSpawnPos, playerBuffer);
    }
}