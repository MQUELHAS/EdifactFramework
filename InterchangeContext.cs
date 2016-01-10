using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace EdifactFramework
{
    /// <summary>
    /// The context of the interchange
    /// Contains the separators, the format and the namespace 
    /// </summary>
    public class InterchangeContext : IEquatable<InterchangeContext>
    {
        private StreamReader _streamReader;

        /// <summary>
        /// Separator for segments
        /// </summary>
        internal string SegmentTerminator { get; private set; }

        /// <summary>
        /// Separator for component data elements
        /// </summary>
        internal string ComponentDataElementSeparator { get; private set; }

        /// <summary>
        /// Release indicator for escaping terminators
        /// </summary>
        internal string ReleaseIndicator { get; private set; }

        ///<summary>
        /// Separator for data elements
        /// </summary>
        internal string DataElementSeparator { get; private set; }

        /// <summary>
        /// Separator for repetitions of data elements
        /// </summary>
        internal string RepetitionSeparator { get; set; }

        /// <summary>
        /// The format of the interchange, e.g. the format of the envelope
        /// </summary>
        private EdiFormats Format { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="InterchangeContext"/> class.
        /// </summary>
        public InterchangeContext()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InterchangeContext"/> class.
        /// This extracts the separators from the contents of the edi message.
        /// </summary>
        /// <param name="contents">The edi message</param>
        public InterchangeContext(string contents)
        {
            if (contents == null) throw new ArgumentNullException(nameof(contents));

            contents = contents.Replace(Environment.NewLine, string.Empty);

            var firstSegmentName = contents.ToUpper().Substring(0, 3);
            switch (firstSegmentName)
            {
                case EdiSegments.Unb:
                    //  Default Edifact separators
                    ComponentDataElementSeparator = ":";
                    DataElementSeparator = "+";
                    ReleaseIndicator = "?";
                    RepetitionSeparator = "*";
                    SegmentTerminator = "'";
                    Format = EdiFormats.Edifact;
                    break;
                case EdiSegments.Una:
                    try
                    {
                        //  Parse UNA separators
                        var una = contents.Replace(EdiSegments.Una, "").Take(6).ToList();

                        ComponentDataElementSeparator = una[0].ToString(CultureInfo.InvariantCulture);
                        DataElementSeparator = una[1].ToString(CultureInfo.InvariantCulture);
                        ReleaseIndicator = una[3].ToString(CultureInfo.InvariantCulture);
                        RepetitionSeparator = "*";
                        SegmentTerminator = una[5].ToString(CultureInfo.InvariantCulture);
                        Format = EdiFormats.Edifact;
                    }
                    catch (Exception ex)
                    {
                        throw new ParserException("Can't find UNA interchange delimiters", ex);
                    }
                    break;
                default:
                    throw new ParserException("Can't identify format by: " + firstSegmentName);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InterchangeContext"/> class.
        /// This gets the default separators per format.
        /// </summary>
        /// <param name="format">The format</param>
        public InterchangeContext(EdiFormats format)
        {
            switch (format)
            {
                case EdiFormats.Edifact:
                    ComponentDataElementSeparator = ":";
                    DataElementSeparator = "+";
                    ReleaseIndicator = "?";
                    RepetitionSeparator = "*";
                    SegmentTerminator = "'";
                    Format = EdiFormats.Edifact;
                    break;
                default:
                    throw new ParserException("Unsupported format: " + format);
            }
        }

        public InterchangeContext(StreamReader _streamReader)
        {
            this._streamReader = _streamReader;

            ComponentDataElementSeparator = ":";
            DataElementSeparator = "+";
            ReleaseIndicator = "?";
            RepetitionSeparator = "*";
            SegmentTerminator = "'";
            Format = EdiFormats.Edifact;
        }

        /// <summary>
        /// If the separators are format default.
        /// </summary>
        public bool IsDefault => Equals(new InterchangeContext(Format));

        /// <summary>
        /// Merges the separators of two interchange contexts when the source separator is not empty or null
        /// </summary>
        /// <param name="context">
        /// The context to merge from
        /// </param>
        public void Merge(InterchangeContext context)
        {
            if (context == null) return;
            // Merge all separators if they are not blank or null
            // All the blank and null separators remain the same
            if (!string.IsNullOrEmpty(context.ComponentDataElementSeparator))
                ComponentDataElementSeparator = context.ComponentDataElementSeparator;
            if (!string.IsNullOrEmpty(context.DataElementSeparator))
                DataElementSeparator = context.DataElementSeparator;
            if (!string.IsNullOrEmpty(context.ReleaseIndicator))
                ReleaseIndicator = context.ReleaseIndicator;
            if (!string.IsNullOrEmpty(context.RepetitionSeparator))
                RepetitionSeparator = context.RepetitionSeparator;
            if (!string.IsNullOrEmpty(context.SegmentTerminator))
                SegmentTerminator = context.SegmentTerminator;
        }

        /// <summary>
        /// Validates an interchange context.
        /// Separators must be unique within the interchange context.
        /// </summary>
        /// <returns>If it is valid</returns>
        public bool IsValid(EdiFormats format)
        {
            if (format == EdiFormats.Edifact)
                if (Format != EdiFormats.Edifact) return false;

            //if (format == EdiFormats.X12)
            //    if (Format != EdiFormats.X12) return false;

            var temp = new List<string>();
            if (!string.IsNullOrEmpty(ComponentDataElementSeparator))
                temp.Add(ComponentDataElementSeparator);
            if (!string.IsNullOrEmpty(DataElementSeparator))
                temp.Add(DataElementSeparator);
            if (!string.IsNullOrEmpty(RepetitionSeparator))
                temp.Add(RepetitionSeparator);
            if (!string.IsNullOrEmpty(SegmentTerminator))
                temp.Add(SegmentTerminator);

            var b = temp.GroupBy(t => t).Where(g => g.Count() > 1).Select(v => v.Key).ToList();
            return !b.Any();
        }

        /// <summary>
        /// Custom equal
        /// Checks if all separators match
        /// </summary>
        /// <param name="other">
        /// Interchange to compare with
        /// </param>
        /// <returns>
        /// If it is equal, e.g. if all separators match
        /// </returns>
        public bool Equals(InterchangeContext other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            return other.ComponentDataElementSeparator == ComponentDataElementSeparator &&
                   other.DataElementSeparator == DataElementSeparator &&
                   other.ReleaseIndicator == ReleaseIndicator &&
                   other.RepetitionSeparator == RepetitionSeparator &&
                   other.SegmentTerminator == SegmentTerminator;
        }

        /// <summary>
        /// Escapes the terminators from a string
        /// </summary>
        /// <param name="line">The string to escape</param>
        /// <returns>The escaped string</returns>
        public string EscapeLine(string line)
        {
            var result = "";

            foreach (var l in line.ToCharArray())
            {
                // If a char is a separator
                if (ContainsTerminator(l))
                {
                    // escape it with the release indicator
                    result = result + ReleaseIndicator + l;
                }
                else
                {
                    result = result + l;
                }
            }

            return result;
        }

        /// <summary>
        /// Checks if a char is a separator
        /// </summary>
        /// <param name="value">The char</param>
        /// <returns>If the char is a separator</returns>
        private bool ContainsTerminator(char value)
        {
            return SegmentTerminator.ToCharArray().Contains(value) ||
                   ComponentDataElementSeparator.ToCharArray().Contains(value) ||
                   DataElementSeparator.ToCharArray().Contains(value) ||
                   RepetitionSeparator.ToCharArray().Contains(value);
        }
    }
}