using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrojanExceptions
{
    internal class ExceptionHandler : Exception
    {
        public ExceptionHandler() {}

        public ExceptionHandler(string message)
        : base(message) {}

        public ExceptionHandler(string message, Exception inner)
        : base(message, inner) {}
    }
}
