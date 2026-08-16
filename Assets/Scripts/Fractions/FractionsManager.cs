using UnityEngine;
using System.Collections.Generic;

public class FractionsManager : MonoBehaviour
{
    public List<Fraction> fractions = new List<Fraction>();

    public Fraction activeFraction;

    void Start()
    {
        activeFraction = fractions[0];
    }

    public void SetActiveFraction(int index)
    {
        if (index >= 0 && index < fractions.Count)
        {
            activeFraction = fractions[index];
        }
    }
}