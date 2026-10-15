


using System.Collections.Generic;
using UnityEngine;
using VaSiLi.Logging;

public class LogManager : MonoBehaviour
{
    public List<ILogCollector<object>> logCollectors;

    void OnEnable()
    {
        foreach (var collector in logCollectors)
        {
            //collector.OnEnable();
        }
    }
}