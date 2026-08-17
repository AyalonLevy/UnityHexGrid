using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public struct BFSResult
{
    public Dictionary<Vector3Int, Vector3Int?> visitedNodesDict;

    public List<Vector3Int> GetPathTo(Vector3Int destination)
    {
        if (visitedNodesDict == null || !visitedNodesDict.ContainsKey(destination))
            return new List<Vector3Int>();

        return GraphSearch.GeneratePathBFS(destination, visitedNodesDict);
    }

    public bool IsHexPositionInRange(Vector3Int position)
    {
        return visitedNodesDict != null && visitedNodesDict.ContainsKey(position);
    }

    public IEnumerable<Vector3Int> GetRangePositions()
    {
        return visitedNodesDict != null ? visitedNodesDict.Keys : Enumerable.Empty<Vector3Int>();
    }
}

public class GraphSearch
{
    public static BFSResult BFSGetRange(GridManager gridManager, Vector3Int startPoint, int movementPoints)
    {
        Dictionary<Vector3Int, Vector3Int?> visitedNodes = new();
        Dictionary<Vector3Int, int> costSoFar = new();
        Queue<Vector3Int> nodesToVisitQueue = new();

        nodesToVisitQueue.Enqueue(startPoint);
        costSoFar[startPoint] = 0;
        visitedNodes[startPoint] = null;

        while (nodesToVisitQueue.Count > 0)
        {
            Vector3Int currentNode = nodesToVisitQueue.Dequeue();

            foreach (HexTileData neighbour in gridManager.GetTileNeighbours(currentNode))
            {
                if (neighbour == null || neighbour.IsObstacle() || !neighbour.isExplored)
                    continue;

                Vector3Int neighbourPosition = neighbour.tileCoordinates;
                int currentCost = costSoFar[currentNode];
                int newCost = currentCost + neighbour.GetCost();

                if (newCost <= movementPoints)
                {
                    if (!visitedNodes.ContainsKey(neighbourPosition))
                    {
                        visitedNodes[neighbourPosition] = currentNode;
                        costSoFar[neighbourPosition] = newCost;
                        nodesToVisitQueue.Enqueue(neighbourPosition);
                    }
                    else if (costSoFar[neighbourPosition] > newCost)    // Found a cheaper path to this node
                    {
                        costSoFar[neighbourPosition] = newCost;
                        visitedNodes[neighbourPosition] = currentNode;
                    }
                }
            }
        }

        return new BFSResult { visitedNodesDict = visitedNodes };
    }

    public static List<Vector3Int> GeneratePathBFS(Vector3Int current, Dictionary<Vector3Int, Vector3Int?> visitedNodesDict)
    {
        List<Vector3Int> path = new();

        // Trace backward from destination to the start point, excluding the start node itself
        while (visitedNodesDict.ContainsKey(current) && visitedNodesDict[current] != null)
        {
            path.Add(current);
            current = visitedNodesDict[current].Value;
        }

        path.Reverse();
        return path;
    }
}
