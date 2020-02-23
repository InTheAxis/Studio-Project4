﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct BuildingCell
{
    public BuildingCell(Vector3 pos, float size, Vector3 offSetDir, bool isLeft, Quaternion rot)
    {
        this.pos = pos;
        //this.min = min;
        //this.max = max;
        //this.minB = new Vector2(min.x)
        this.radius = size;
        this.offSetDir = offSetDir;
        this.isLeft = isLeft;
        this.rot = rot;
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

    //[SerializeField]
    //private float minOffset;

    //[SerializeField]
    //private float maxOffset;
    [SerializeField]
    private MinMax buffer;

    [SerializeField]
    private MinMax cellRadius;

    //[SerializeField]
    //private MinMax width;

    [SerializeField]
    private float initialSpace;

    [SerializeField]
    private RoadGenerator roadGenerator;
    [SerializeField]
    private TowerGenerator towerGenerator;

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
                    float currCellRadius = cellRadius.Get();
                    //float cellWidth = width.Get();
                    float spacing = buffer.Get();
                    currDist += currCellRadius;
                    currPos = currDist * dir + startPos;
                    //Vector3 end = currPos + dir * cellLength;
                    //Vector3 cellMax = end + pDir * cellWidth;
                    // TODO: check for tower
                    //
                    // create cell
                    cells.Add(new BuildingCell(currPos, currCellRadius, pDir, isLeft, rot));
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
                Gizmos.color = Color.red;
            else
                Gizmos.color = Color.green;
            Gizmos.DrawSphere(cell.pos, 2);
        }
    }
}