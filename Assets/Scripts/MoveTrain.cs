namespace IAV23.ElisaTodd
{
    using System.Collections;
    using System.Collections.Generic;
    using TMPro;
    using UnityEngine;

    public class MoveTrain : MonoBehaviour
    {
        [SerializeField] private Transform train;
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float delayTime = 0f;
        [SerializeField] private float rotationSpeed = 5f;

        public IEnumerator Move(List<Vertex> path)
        {
            foreach (Vertex vertex in path)
            {
                Vector3 vertexPos = vertex.transform.position;
                yield return StartCoroutine(MoveToPosition(vertexPos));
                yield return new WaitForSeconds(delayTime);
            }
        }
        private IEnumerator MoveToPosition(Vector3 targetPosition)
        {
            while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
            {
                Vector3 auxPosition = targetPosition;
                auxPosition.y = transform.position.y;
                train.forward = (auxPosition - train.position).normalized;
                    
                train.position = Vector3.MoveTowards(train.position, targetPosition, moveSpeed * Time.deltaTime);

                yield return null;
            }

            transform.position = targetPosition;
        }
    }
}
