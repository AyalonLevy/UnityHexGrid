using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[SelectionBase]
public class Unit : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private int movementPoints = 20;

    public int MovementPoints { get => movementPoints; }

    [SerializeField] private float movementDuration = 1.0f;
    [SerializeField] private float rotationDuration = 0.3f;

    public HexTileData CurrentTile { get; set; }

    private Highlight highlight;
    private Queue<Vector3> pathPositions = new();

    public event Action<Unit> MovementFinished;

    private void Awake()
    {
        highlight = GetComponent<Highlight>();

        if (highlight != null)
        {
            highlight.Initialize(transform);
        }
    }

    internal void Deselect()
    {
        highlight.SetHighlight(false);
    }

    public void Select()
    {
        highlight.SetHighlight(true);
    }

    internal void MoveThroughPath(List<Vector3> currentPath)
    {
        pathPositions = new(currentPath);
        Vector3 firstTarget = pathPositions.Dequeue();
        StartCoroutine(RotatopnCoroutine(firstTarget, rotationDuration));
    }

    private IEnumerator RotatopnCoroutine(Vector3 endPosition, float rotationDuration)
    {
        Quaternion startRotation = transform.rotation;
        endPosition.y = transform.position.y;
        Vector3 direction = endPosition - transform.position;
        Quaternion endRotation = Quaternion.LookRotation(direction, Vector3.up);

        if (Mathf.Approximately(Mathf.Abs(Quaternion.Dot(startRotation, endRotation)), 1.0f) == false)
        {
            float elapsedTime = 0;
            while (elapsedTime < rotationDuration)
            {
                elapsedTime += Time.deltaTime;
                float lerpStep = elapsedTime / rotationDuration;
                transform.rotation = Quaternion.Lerp(startRotation, endRotation, lerpStep);
                yield return null;
            }

            transform.rotation = endRotation;
        }

        StartCoroutine(MovementCoroutine(endPosition));
    }

    private IEnumerator MovementCoroutine(Vector3 endPosition)
    {
        Vector3 startPosition = transform.position;
        endPosition.y = startPosition.y;
        float elapsedTime = 0;

        while (elapsedTime < movementDuration)
        {
            elapsedTime += Time.deltaTime;
            float lerpStep = elapsedTime / movementDuration;
            transform.position = Vector3.Lerp(startPosition, endPosition, lerpStep);
            yield return null;
        }

        transform.position = endPosition;

        if (pathPositions.Count > 0)
        {
            Debug.Log("Selecting the next position!");
            StartCoroutine(RotatopnCoroutine(pathPositions.Dequeue(), rotationDuration));
        }
        else
        {
            Debug.Log("Movement finished!");
            MovementFinished?.Invoke(this);
        }
    }
}
