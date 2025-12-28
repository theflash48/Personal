using System.Collections;
using System.Collections.Generic;
using Unity.Netcode.Components;
using UnityEditor.ShaderGraph;
using UnityEngine;


public class ClientNetworkTransform : NetworkTransform
{
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}
