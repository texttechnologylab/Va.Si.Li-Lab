using System;

namespace VaSiLi.Janus
{
    public class JanusException : Exception
    {
        public int Code { get; }
        public JanusException(string message) : base(message) { }
        public JanusException(int code, string reason) : base($"Janus error {code}: {reason}")
        {
            Code = code;
        }
    }

    public class JanusTransportException : Exception
    {
        public JanusTransportException(string message) : base(message) { }
        public JanusTransportException(string message, Exception inner) : base(message, inner) { }
    }
}
