namespace IAV23.ElisaTodd
{
    using System;
    using UnityEngine;

    // Puntos representativos o v�rtice (com�n a todos los esquemas de divisi�n, o a la mayor�a de ellos)
    [System.Serializable]
    public class Vertex : MonoBehaviour, IComparable<Vertex>
    {
        /// <summary>
        /// Identificador del v�rtice 
        /// </summary>
        public int id;

        /// <summary>
        /// Coste del v�rtice 
        /// </summary>
        public float cost;

        /// <summary>
        /// Es un v�rtice importante dentro del conjunto de v�rtices
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


