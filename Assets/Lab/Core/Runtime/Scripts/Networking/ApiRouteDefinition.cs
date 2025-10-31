using System;
using UnityEngine;

[Serializable]
[CreateAssetMenu(fileName = "Connection", menuName = "VaSiLi/API Route Definition", order = 1)]
public class ApiRouteDefinition : ScriptableObject
{
    public string host;
    public string port;
    public string route;

    public override string ToString()
    {
        if (route == null) 
            return $"{host}:{port}";
        return $"{host}:{port}/{route}";
    }
}