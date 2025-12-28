using UnityEngine;
using System;

/// <summary>
/// Estado dinámico de la carrera: cronómetro, bandera y finalización.
/// </summary>
public class RaceState : MonoBehaviour
{
    public enum FlagState { Red, Yellow, GreenBlink, Green, White, Black }

    [SerializeField]
    private RaceConfig config; // Referencia inyectada vía inspector

    public FlagState currentFlag { get; private set; } = FlagState.Red;
    public float elapsedTime { get; private set; } = 0f;
    public bool raceFinished { get; private set; } = false;

    /// <summary>Evento disparado cuando cambia el cronómetro.</summary>
    public event Action<float> OnTimeUpdated;
    /// <summary>Evento disparado al cambiar la bandera.</summary>
    public event Action<FlagState> OnFlagChanged;
    /// <summary>Evento disparado al terminar la carrera.</summary>
    public event Action OnRaceFinished;

    private void Awake()
    {
        if (config == null)
            Debug.LogError("RaceState requiere una referencia a RaceConfig.");
    }

    private void Update()
    {
        if (raceFinished || config == null)
            return;

        elapsedTime += Time.deltaTime;
        OnTimeUpdated?.Invoke(elapsedTime);

        // Modo tiempo: finalizar
        if (!config.IsLapMode && elapsedTime >= config.targetTime)
        {
            FinishRace();
        }
    }

    /// <summary>
    /// Cambia el estado de la bandera y notifica.
    /// </summary>
    public void SetFlag(FlagState newFlag)
    {
        currentFlag = newFlag;
        OnFlagChanged?.Invoke(newFlag);
    }

    private void FinishRace()
    {
        raceFinished = true;
        OnRaceFinished?.Invoke();
    }

    /// <summary>
    /// Reinicia el estado para una nueva carrera.
    /// </summary>
    public void ResetState()
    {
        elapsedTime = 0f;
        raceFinished = false;
        SetFlag(FlagState.Red);
    }
}