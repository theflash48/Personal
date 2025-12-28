using System;
using UnityEngine;

/// <summary>
/// Representa a un piloto inscrito en la carrera.
/// </summary>
[Serializable]
public struct Competitor
{
    public string shortName;   // 3 letras
    public string longName;    // Nombre completo
    public byte carNumber;     // Número del coche
    public Color color;        // Color para UI
    public int laps;           // Vueltas completadas
    public bool dnf;           // No terminó
}