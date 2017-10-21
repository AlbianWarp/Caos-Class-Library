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

    public class NoGameCaosException : CaosExpection
    {
        public NoGameCaosException()
        { }

        public NoGameCaosException(string message) : base(message)
        { }

        public NoGameCaosException(string message, Exception innerException) : base(message, innerException)
        { }
    }
}
