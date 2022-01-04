using System;

namespace Trojan.exceptions
{
    internal class ExceptionHandler : Exception
    {
        public ExceptionHandler() { }

        public ExceptionHandler(string message)
        : base(message) { }

        public ExceptionHandler(string message, Exception inner)
        : base(message, inner) { }
    }
}
