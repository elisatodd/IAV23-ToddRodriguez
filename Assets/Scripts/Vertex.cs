namespace IAV23.ElisaTodd
{
    using System;
    using UnityEngine;

    // Puntos representativos o vértice (común a todos los esquemas de división, o a la mayoría de ellos)
    [System.Serializable]
    public class Vertex : MonoBehaviour, IComparable<Vertex>
    {
        /// <summary>
        /// Identificador del vértice 
        /// </summary>
        public int id;

        /// <summary>
        /// Coste del vértice 
        /// </summary>
        public float cost;

        /// <summary>
        /// Es un vértice importante dentro del conjunto de vértices
        /// </summary>
        public bool essential = false;

        public int CompareTo(Vertex other)
        {
            float result = this.cost - other.cost;
            return (int)(Mathf.Sign(result) * Mathf.Ceil(Mathf.Abs(result)));
        }

        public bool Equals(Vertex other)
        {
            return (other.id == this.id);
        }

        public override bool Equals(object obj)
        {
            Vertex other = (Vertex)obj;
            if (ReferenceEquals(obj, null)) return false;
            return (other.id == this.id);
        }
    }
}


