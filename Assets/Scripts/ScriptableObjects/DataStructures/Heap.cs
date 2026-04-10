using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Heap", menuName = "ScriptableObjects/Utility/Heap", order = 1)]
public class Heap : ScriptableObject
{
    public int bigInt = 99999;
    public int size;
    // Tiles numbers
    public List<int> tiles;
    // Distances/Move Costs
    public List<int> weights;


    public void ResetHeap()
    {
        size = 0;
        tiles.Clear();
        weights.Clear();
    }

    protected void IncreaseSize()
    {
        tiles.Add(-1);
        weights.Add(bigInt);
    }

    public void InitializeHeap(int newSize)
    {
        tiles = new List<int>(new int[newSize]);
        weights = new List<int>(new int[newSize]);
        for (int i = 0; i < newSize; i++)
        {
            tiles[i] = -1;
            weights[i] = bigInt;
        }
    }

    protected void EnsureCapacity()
    {
        if (weights.Count < size + 1)
        {
            for (int i = 0; i < size; i++)
            {
                IncreaseSize();
            }
        }
    }

    protected int getLeftChildIndex(int parentIndex)
    {
        return ((parentIndex*2)+1);
    }

    protected int getRightChildIndex(int parentIndex)
    {
        return ((parentIndex*2)+2);
    }

    protected int getParentIndex(int childIndex)
    {
        return ((childIndex-1)/2);
    }

    protected bool hasLeftChild(int index)
    {
        return (getLeftChildIndex(index) < size);
    }

    protected bool hasRightChild(int index)
    {
        return (getRightChildIndex(index) < size);
    }

    protected bool hasParent(int index)
    {
        if (index <= 0)
        {
            return false;
        }
        return true;
    }
    // Child/Parents are sorted by weight.

    protected int leftChild(int index)
    {
        return weights[getLeftChildIndex(index)];
    }

    protected int rightChild(int index)
    {
        return weights[getRightChildIndex(index)];
    }

    protected int parent(int index)
    {
        return weights[getParentIndex(index)];
    }

    // Need to swap on all lists.
    protected void Swap(int indexOne, int indexTwo)
    {
        int temp = weights[indexOne];
        weights[indexOne] = weights[indexTwo];
        weights[indexTwo] = temp;
        int temp2 = tiles[indexOne];
        tiles[indexOne] = tiles[indexTwo];
        tiles[indexTwo] = temp2;
    }

    // When looking/pulling you care about the actual tile not the move cost.
    public int Peek()
    {
        if (size == 0)
        {
            return -1;
        }
        return tiles[0];
    }

    public int PeekWeight()
    {
        if (size == 0)
        {
            return -1;
        }
        return weights[0];
    }

    public int Pull()
    {
        if (size == 0)
        {
            return -1;
        }
        int tile = tiles[0];
        tiles[0] = tiles[size-1];
        weights[0] = weights[size-1];
        size--;
        HeapifyDown();
        return tile;
    }

    public void AddNodeWeight(int newNode, int newWeight)
    {
        EnsureCapacity();
        weights[size] = newWeight;
        tiles[size] = newNode;
        size++;
        HeapifyUp();
    }

    protected void HeapifyUp()
    {
        int index = size - 1;
        while (parent(index) > weights[index])
        {
            Swap(getParentIndex(index), index);
            index = getParentIndex(index);
        }
    }

    protected void HeapifyDown()
    {
        int index = 0;
        while (hasLeftChild(index))
        {
            int smallerChildIndex = getLeftChildIndex(index);
            if (hasRightChild(index) && weights[getRightChildIndex(index)] < weights[smallerChildIndex])
            {
                smallerChildIndex = getRightChildIndex(index);
            }
            if (weights[index] < weights[smallerChildIndex])
            {
                break;
            }
            Swap(index, smallerChildIndex);
            index = smallerChildIndex;
        }
    }
}
