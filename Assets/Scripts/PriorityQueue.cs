using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Implementación de una Cola de Prioridad con el uso de BinaryHeap.
/// </summary>
/// <typeparam name="T"></typeparam>
public class PriorityQueue<T> where T : IComparable<T>
{
    BinaryHeap<T> heap;

    public PriorityQueue() {
        heap = new BinaryHeap<T>();
    }

    public void Push(T node)
    {
        heap.Add(node);
    }

    public T Top() { return heap.Top; }

    public T Pop()
    {
        return heap.Remove();
    }

    public bool Contains(T N)
    {
        if (heap == null || heap.Count == 0)
            return false;
        return heap.Contains(N);
    }

    public bool Empty()
    {
        return heap.Count == 0;
    }

    public T Find(T a)
    {
        return heap.Find(a);
    }

    public bool Remove(T a)
    {
        return heap.Remove(a);
    }
}
