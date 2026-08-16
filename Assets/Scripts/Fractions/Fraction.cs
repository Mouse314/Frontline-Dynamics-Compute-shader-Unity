using UnityEngine;

[CreateAssetMenu(fileName = "New Fraction", menuName = "Game/FractionData")]
public class Fraction : ScriptableObject
{
    public int fractionID;
    public Color fractionColor;
    public string fractionName;
}