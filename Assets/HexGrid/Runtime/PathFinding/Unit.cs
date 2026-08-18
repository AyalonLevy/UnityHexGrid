namespace HexGrid
{
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

        [Header("Selection Indicator Settings")]
        [Tooltip("Reference to the 2D ground indicator GameObject.")]
        [SerializeField] private GameObject selectionIndicator;

        public HexTileData CurrentTile { get; set; }

        private Queue<Vector3> pathPositions = new();

        public event Action<Unit> MovementFinished;

        private void Awake()
        {
            Deselect();
        }

        internal void Deselect()
        {
            if (selectionIndicator != null)
            {
                selectionIndicator.SetActive(false);
            }
        }

        public void Select()
        {
            if (selectionIndicator != null)
            {
                selectionIndicator.SetActive(true);
            }
        }

        internal void MoveThroughPath(List<Vector3> currentPath)
        {
            if (currentPath == null || currentPath.Count == 0) return;

            pathPositions.Clear();
            foreach (var pos in currentPath)
            {
                pathPositions.Enqueue(pos);
            }

            Vector3 firstTarget = pathPositions.Dequeue();
            StartCoroutine(RotationCoroutine(firstTarget, rotationDuration));
        }

        private IEnumerator RotationCoroutine(Vector3 endPosition, float rotationDuration)
        {
            Quaternion startRotation = transform.rotation;
            endPosition.y = transform.position.y;
            Vector3 direction = endPosition - transform.position;

            if (direction != Vector3.zero)
            {
                Quaternion endRotation = Quaternion.LookRotation(direction, Vector3.up);

                if (!Mathf.Approximately(Mathf.Abs(Quaternion.Dot(startRotation, endRotation)), 1.0f))
                {
                    float elapsedTime = 0;
                    while (elapsedTime < rotationDuration)
                    {
                        elapsedTime += Time.deltaTime;
                        float lerpStep = Mathf.Clamp01(elapsedTime / rotationDuration);
                        transform.rotation = Quaternion.Lerp(startRotation, endRotation, lerpStep);
                        yield return null;
                    }

                    transform.rotation = endRotation;
                }
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
                float lerpStep = Mathf.Clamp01(elapsedTime / movementDuration);
                transform.position = Vector3.Lerp(startPosition, endPosition, lerpStep);
                yield return null;
            }

            transform.position = endPosition;

            if (pathPositions.Count > 0)
            {
                StartCoroutine(RotationCoroutine(pathPositions.Dequeue(), rotationDuration));
            }
            else
            {
                MovementFinished?.Invoke(this);
            }
        }
    }
}