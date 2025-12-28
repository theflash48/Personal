using UnityEngine;
using System;

/// <summary>
/// Configuración de la carrera: objetivo en vueltas o en tiempo.
/// </summary>
[CreateAssetMenu(menuName = "Race/Config")]
public class RaceConfig : ScriptableObject
{
    [Tooltip("Número de vueltas objetivo. Si es 0, se usa modo tiempo.")]
    public int targetLaps;

    [Tooltip("Tiempo objetivo en segundos. Si es 0, se usa modo vueltas.")]
    public float targetTime;

    /// <summary>Evento disparado cuando se actualiza la configuración.</summary>
    public event Action OnConfigChanged;

    /// <summary>
    /// Invoca el evento de cambio de configuración.
    /// </summary>
    public void NotifyConfigChanged()
    {
        OnConfigChanged?.Invoke();
    }

    public bool IsLapMode => targetLaps > 0;
}