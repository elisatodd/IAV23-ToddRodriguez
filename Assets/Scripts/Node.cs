using System;

namespace IAV23.ElisaTodd
{
    public class Node : IComparable<Node>
    {

        public int vertexId; // current path vertex id
        public Node prevNode; // guardamos el nodo completo en lugar de solo el id del vértice
        public float costSoFar; // cost from temp path made
        public float estimatedTotalCost; // estimate cost 

        public int CompareTo(Node other)
        {
            return (int)(this.estimatedTotalCost - other.estimatedTotalCost);
        }

        //Modificado
        public bool Equals(Node other)
        {
            if (other == null) return false;
            return (other.vertexId == this.vertexId);
        }

        //Modificado
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            Node other = (Node)obj;
            return (other.vertexId == this.vertexId);
        }

        public override int GetHashCode()
        {
            return this.vertexId.GetHashCode();
        }

    }
}