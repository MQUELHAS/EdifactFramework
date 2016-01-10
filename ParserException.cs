using System;

namespace EdifactFramework
{
    /// <summary>
    /// Parser exception.
    /// Thrown for all known exception conditions with a "friendly" mesage
    /// </summary>
    [Serializable]
    public class ParserException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ParserException"/> class.
        /// </summary>
        public ParserException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParserException"/> class.
        /// </summary>
        /// <param name="message">
        /// Custom message
        /// </param>
        public ParserException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParserException"/> class.
        /// </summary>
        /// <param name="message">
        /// Custom message
        /// </param>
        /// <param name="inner">
        /// The inner exception
        /// </param>
        public ParserException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}