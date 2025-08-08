using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RecursiveRendererSort : MonoBehaviour {

    public int sortOrder;

    void Start()
    {
        ChangeSortingOrder(sortOrder);
    }

    public void ChangeSortingOrder (int order)
    {
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            r.sortingOrder += order;
        }
    }

    public void SetSortingOrder(int order)
    {
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            r.sortingOrder = order;
        }
    }
}
