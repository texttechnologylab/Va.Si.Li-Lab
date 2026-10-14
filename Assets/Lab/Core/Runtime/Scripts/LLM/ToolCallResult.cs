using System;
using UnityEngine;

namespace VaSiLi.Llm {
    [Serializable]
    public class ToolCallResult {
        public string id;
        public bool ok;
        public object Result;
        public string error;
        
        public ToolCallResult(string id, bool ok, object result, string error)
        {
            this.id = id;
            this.ok = ok;
            this.Result = result;
            this.error = error;
        }
    }
}
