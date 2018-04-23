namespace CouchBaseDocumentExpiry.DocumentExpiry
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class UnableToDetermineKeyTypeException : Exception
    {
        public UnableToDetermineKeyTypeException()
        {
        }

        public UnableToDetermineKeyTypeException(string message) : base(message)
        {
        }

        public UnableToDetermineKeyTypeException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected UnableToDetermineKeyTypeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}