using System;

namespace CAOS
{
    public class CaosExpection : Exception
    {
        public CaosExpection()
        { }

        public CaosExpection(string message) : base(message)
        { }

        public CaosExpection(string message, Exception innerException) : base(message, innerException)
        { }
    }

    public class NoRunningEngineException : CaosExpection
    {
        public NoRunningEngineException()
        { }

        public NoRunningEngineException(string message) : base(message)
        { }

        public NoRunningEngineException(string message, Exception innerException) : base(message, innerException)
        { }
    }

    public class UnexpectedEngineOutputException : CaosExpection
    {
        public UnexpectedEngineOutputException()
        { }

        public UnexpectedEngineOutputException(string message) : base(message)
        { }

        public UnexpectedEngineOutputException(string message, Exception innerException) : base(message, innerException)
        { }
    }
}
