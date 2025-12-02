using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResultsCollection
{
    public class ResultsCollectionException : Exception
    {
        public ResultsCollectionException(string message, Exception innerException) : base(message, innerException) { }
        public ResultsCollectionException(string message) : base(message) { }
        public ResultsCollectionException() : base() { }
    }
}
