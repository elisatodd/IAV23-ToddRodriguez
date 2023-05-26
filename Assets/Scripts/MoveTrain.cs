namespace IAV23.ElisaTodd
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Clase que se encarga de desplazar el tren por el mapa
    /// </summary>
    public class MoveTrain : MonoBehaviour
    {
        [SerializeField] private Transform train;
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float delayTime = 0f;
        [SerializeField] private float rotationSpeed = 5f;

        /// <summary>
        /// Mueve el tren através de unos vértices usando una corrutina
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public IEnumerator Move(List<Vertex> path)
        {
            foreach (Vertex vertex in path)
            {
                Vector3 vertexPos = vertex.transform.position;
                yield return StartCoroutine(MoveToPosition(vertexPos));

                GameManager.instance.GasLevel -= (int)vertex.cost;
                GameManager.instance.GasLevel += vertex.gas;

                GameManager.instance.UpdateGasUI();

                yield return new WaitForSeconds(delayTime);
            }
        }

        /// <summary>
        /// Mueve el tren hasta una posición determinada, usando una corrutina
        /// </summary>
        /// <param name="targetPosition"> Posición a la que se quiere mover el tren </param>
        /// <returns></returns>
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
