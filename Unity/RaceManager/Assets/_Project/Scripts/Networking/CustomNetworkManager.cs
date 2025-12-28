using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

/// <summary>
/// Administra las salas y la sincronización de la carrera en el servidor.
/// Basado en eventos y evitando FindObjectOfType.
/// </summary>
public class CustomNetworkManager : NetworkManager
{
    // Evento disparado al crear una sala nueva (código)
    public static event Action<string> OnRoomCreated;
    // Evento disparado cuando un cliente se une a una sala (código, connectionId)
    public static event Action<string, int> OnClientJoinedRoom;
    // Evento disparado cuando la última conexión abandona la sala (código)
    public static event Action<string> OnRoomClosed;

    // Map sala -> lista de connections
    private readonly Dictionary<string, List<NetworkConnectionToClient>> rooms = new();

    /// <summary>
    /// Comando recibido desde un cliente para crear una sala con código.
    /// </summary>
    [Command(requiresAuthority = false)]
    public void CmdCreateRoom(string code)
    {
        if (rooms.ContainsKey(code))
        {
            TargetError(connectionToClient, $"La sala {code} ya existe.");
            return;
        }
        rooms[code] = new List<NetworkConnectionToClient> { connectionToClient };
        OnRoomCreated?.Invoke(code);
        TargetRoomCreated(connectionToClient, code);
    }

    /// <summary>
    /// Comando recibido desde un cliente para unirse a sala.
    /// </summary>
    [Command(requiresAuthority = false)]
    public void CmdJoinRoom(string code)
    {
        if (!rooms.TryGetValue(code, out var list))
        {
            TargetError(connectionToClient, $"La sala {code} no existe.");
            return;
        }
        list.Add(connectionToClient);
        OnClientJoinedRoom?.Invoke(code, connectionToClient.connectionId);
        TargetJoinedRoom(connectionToClient, code);
    }

    /// <summary>
    /// Cuando un cliente se desconecta, limpia la sala si queda vacía.
    /// </summary>
    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        base.OnServerDisconnect(conn);
        foreach (var kvp in rooms)
        {
            if (kvp.Value.Remove(conn) && kvp.Value.Count == 0)
            {
                string code = kvp.Key;
                rooms.Remove(code);
                OnRoomClosed?.Invoke(code);
                break;
            }
        }
    }

    // RPCs dirigidos al cliente para confirmación o error
    [TargetRpc]
    private void TargetRoomCreated(NetworkConnection target, string code)
    {
        // Cliente implementa listener para OnRoomCreated local
    }

    [TargetRpc]
    private void TargetJoinedRoom(NetworkConnection target, string code)
    {
        // Cliente implementa listener para OnClientJoined local
    }

    [TargetRpc]
    private void TargetError(NetworkConnection target, string message)
    {
        // Cliente muestra mensaje de error
    }
}
