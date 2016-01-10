using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EdifactFramework
{
    /// <summary>
    /// General helper
    /// </summary>
    public static class EdiHelper
    {
        /// <summary>
        /// Converts a stream into string
        /// </summary>
        /// <param name="ediStream">The stream</param>
        /// <returns>The string</returns>
        public static string ToEdiString(this Stream ediStream)
        {
            if (ediStream == null) throw new ArgumentNullException("ediStream");

            using (var reader = new StreamReader(ediStream, false))
            {
                return reader.ReadToEnd();
            }
        }

        /// <summary>
        /// Converts a stream into string
        /// </summary>
        /// <param name="ediStream">The stream</param>
        /// <param name="encoding">Encoding of the stream</param>
        /// <returns>The string</returns>
        public static string ToEdiString(this Stream ediStream, System.Text.Encoding encoding)
        {
            if (ediStream == null) throw new ArgumentNullException("ediStream");

            using (var reader = new StreamReader(ediStream, encoding, false))
            {
                return reader.ReadToEnd();
            }
        }

        /// <summary>
        /// Splits an Edi messages into a collection of segments
        /// </summary>
        /// <param name="contents">The edi message</param>
        /// <param name="interchangeContext">The interchange context containing the separators</param>
        /// <returns>The collection of segments</returns>
        public static string[] GetEdiSegments(string contents, InterchangeContext interchangeContext)
        {
            if (contents == null) throw new ArgumentNullException("contents");
            if (interchangeContext == null) throw new ArgumentNullException("interchangeContext");

            // Handle carriage return\new line separator
            if (interchangeContext.SegmentTerminator != Environment.NewLine)
                contents = contents.Replace(Environment.NewLine, string.Empty);

            // Handle escape segment separator
            // Removes blank lines
            var result = !string.IsNullOrEmpty(interchangeContext.ReleaseIndicator)
                ? contents.EscapeSplit(interchangeContext.ReleaseIndicator[0],
                    interchangeContext.SegmentTerminator[0],
                    StringSplitOptions.RemoveEmptyEntries)
                : contents.Split(interchangeContext.SegmentTerminator.ToCharArray(),
                    StringSplitOptions.RemoveEmptyEntries);

            result = (from r in result select r.Trim()).ToArray();
            return result;

        }

        /// <summary>
        /// Splits a segment line into composite data elements
        /// </summary>
        /// <param name="ediSegment">The segment line</param>
        /// <param name="interchangeContext">The interchange context containing the separators</param>
        /// <returns>The collection of composite data elements</returns>
        public static string[] GetEdiCompositeDataElements(string ediSegment, InterchangeContext interchangeContext)
        {
            if (string.IsNullOrEmpty(ediSegment)) throw new ArgumentNullException("ediSegment");
            if (interchangeContext == null) throw new ArgumentNullException("interchangeContext");

            // Handle escapes
            // Retains blank lines
            var result = !string.IsNullOrEmpty(interchangeContext.ReleaseIndicator)
                                      ? ediSegment.EscapeSplit(interchangeContext.ReleaseIndicator[0],
                                                             interchangeContext.DataElementSeparator[0], StringSplitOptions.None)
                                      : ediSegment.Split(interchangeContext.DataElementSeparator.ToCharArray(),
                                                       StringSplitOptions.None);

            // Skip the segment name
            return result.Skip(1).ToArray();
        }

        /// <summary>
        /// Splits a segment line into component data elements
        /// </summary>
        /// <param name="ediCompositeDataElement">The composite data element line</param>
        /// <param name="interchangeContext">The interchange context containing the separators</param>
        /// <returns>The collection of data elements</returns>
        public static string[] GetEdiComponentDataElements(string ediCompositeDataElement, InterchangeContext interchangeContext)
        {
            if (interchangeContext == null) throw new ArgumentNullException("interchangeContext");
            if (string.IsNullOrEmpty(ediCompositeDataElement)) throw new ArgumentNullException("ediCompositeDataElement");

            // Handle escapes
            // Retains blank lines
            var result = !string.IsNullOrEmpty(interchangeContext.ReleaseIndicator)
                                      ? ediCompositeDataElement.EscapeSplit(interchangeContext.ReleaseIndicator[0],
                                                             interchangeContext.ComponentDataElementSeparator[0], StringSplitOptions.None)
                                      : ediCompositeDataElement.Split(interchangeContext.ComponentDataElementSeparator.ToCharArray(),
                                                       StringSplitOptions.None);
            return result;
        }

        public static string[] GetRepetitions(string value, InterchangeContext interchangeContext)
        {
            if (!string.IsNullOrEmpty(interchangeContext.ReleaseIndicator))
                return value.EscapeSplit(interchangeContext.ReleaseIndicator[0],
                    interchangeContext.RepetitionSeparator[0], StringSplitOptions.None);

            return value.Split(interchangeContext.RepetitionSeparator.ToCharArray());
        }

        /// <summary>
        /// Gets the segment name from a segment line
        /// </summary>
        /// <param name="ediSegment">The segment line</param>
        /// <param name="interchangeContext">The interchange context containing the separators</param>
        /// <returns>The segment name</returns>
        public static string GetSegmentName(string ediSegment, InterchangeContext interchangeContext)
        {
            if (string.IsNullOrEmpty(ediSegment)) throw new ArgumentNullException("ediSegment");
            if (interchangeContext == null) throw new ArgumentNullException("interchangeContext");
            return ediSegment.StartsWith(EdiSegments.Una) ? EdiSegments.Una : ediSegment.Split(interchangeContext.DataElementSeparator.ToCharArray(), StringSplitOptions.None)[0];
        }

        /// <summary>
        /// Escapes characters in a string
        /// </summary>
        /// <param name="contents">The string to be escaped</param>
        /// <param name="escapeCharacter">The escape character</param>
        /// <param name="splitSeparator">The spli separator</param>
        /// <param name="splitOption">The split option</param>
        /// <returns>The splitted string</returns>
        private static string[] EscapeSplit(this string contents, char escapeCharacter, char splitSeparator, StringSplitOptions splitOption)
        {
            var result = new List<string>();
            var line = "";
            char previousSymbol = char.MinValue;

            // Iterate through all chars in the string
            // This builds a line until the split separator is reached
            // Only if the split separator is not escaped, e.g. not preceded by the escape character
            foreach (char symbol in contents)
            {
                // If the current char is the split separator
                if (symbol == splitSeparator)
                {
                    // Check if the separator is escaped
                    if (previousSymbol != escapeCharacter)
                    {
                        // If it not escaped, add the currently built line
                        // and start the next line
                        // check for escaping the escape character
                        if (line.EndsWith(new string(new[] { escapeCharacter, escapeCharacter }), StringComparison.Ordinal))
                            line = line.Remove(line.Length - 1);

                        result.Add(line);
                        line = "";
                        previousSymbol = char.MinValue;

                        continue;
                    }

                    // Keep building the line until a separator is reached
                    line = line.TrimEnd(escapeCharacter);
                }

                line = line + symbol;

                // Keep track of the previous character in case it's an escape character
                if (previousSymbol == symbol && previousSymbol == escapeCharacter)
                    previousSymbol = char.MinValue;
                else
                    previousSymbol = symbol;
            }

            result.Add(line);

            // Handle blank lines
            if (splitOption == StringSplitOptions.RemoveEmptyEntries)
            {
                result.RemoveAll(string.IsNullOrEmpty);
            }

            return result.ToArray();
        }

        public static string ReadSegment(this TextReader reader, string escapeCharacter, string segmentSeparator)
        {
            var line = "";

            if (escapeCharacter == null)
            {
                escapeCharacter = "?";
            }

            if (segmentSeparator == null)
            {
                segmentSeparator = "'";
            }

            while (reader.Peek() >= 0)
            {
                var symbol = (char)reader.Read();

                if (char.IsControl(symbol))
                {
                    continue;
                }

                line = line + symbol;

                if (!line.EndsWith(segmentSeparator, StringComparison.Ordinal)) continue;
                if (!string.IsNullOrEmpty(escapeCharacter) &&
                    line.EndsWith(string.Concat(escapeCharacter, segmentSeparator), StringComparison.Ordinal))
                {
                    continue;
                }

                //if (segmentSeparator != Environment.NewLine)
                //{
                //    line = line.Replace(Environment.NewLine, string.Empty);
                //    line = line.Replace("\n", string.Empty);
                //}

                var index = line.LastIndexOf(segmentSeparator, StringComparison.Ordinal);
                if (index > 0)
                {
                    line = line.Remove(index);
                }

                if (!string.IsNullOrEmpty(line))
                    break;
            }

            return line + segmentSeparator;
        }

    }
}
