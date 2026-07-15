using UnityEngine;
using System.Reflection;

public class ToolBinding
{
    public MonoBehaviour Source;
    public MethodInfo Method;
    public AgentToolAttribute Attribute;

    public ToolBinding(MonoBehaviour source, MethodInfo method, AgentToolAttribute attribute)
    {
        Source = source;
        Method = method;
        Attribute = attribute;
    }
}