using System;

[AttributeUsage(AttributeTargets.Method)]
public class AgentToolAttribute : Attribute
{
    public string Name;
    public string Description;

    public AgentToolAttribute(string name, string description = null)
    {
        Name = name;
        Description = description;
    }
}