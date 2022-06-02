using System;
using System.Runtime.Serialization;

namespace Natori.Unity.PlayerLoop
{
    public class NatoriPlayerLoopException : Exception
    {
        public NatoriPlayerLoopException()
        {
        }

        protected NatoriPlayerLoopException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public NatoriPlayerLoopException(string message) : base(message)
        {
        }

        public NatoriPlayerLoopException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}