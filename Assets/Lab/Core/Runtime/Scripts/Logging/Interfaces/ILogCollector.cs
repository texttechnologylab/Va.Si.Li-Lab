
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VaSiLi.Logging {
    public interface ILogCollector<T>
    {
        [SerializeField]
        public string Endpoint { get; }
        public T GetMessage();
    }
}