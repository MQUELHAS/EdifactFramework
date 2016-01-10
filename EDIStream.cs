using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EdifactFramework
{
    public class EdiStream : IDisposable
    {
        private readonly InterchangeContext _interchangeContext;
        private readonly StreamReader _streamReader;
        private readonly List<string> _envelope = new List<string>();

        public EdiStream(Stream ediStream)
        {
            _streamReader = new StreamReader(ediStream);
            _interchangeContext = new InterchangeContext(_streamReader);
            ediStream.Position = 0;
            _streamReader.DiscardBufferedData();
        }

        public List<string> Message { get; private set; }
        public object InterchangeHeader { get; private set; }
        public object InterchangeGroup { get; private set; }

        public bool GetNextMessage()
        {
            var message = new List<string>();
            var result = false;

            while (_streamReader.Peek() >= 0 && !result)
            {
                var segment = _streamReader.ReadSegment(_interchangeContext.ReleaseIndicator,
                    _interchangeContext.SegmentTerminator);
                if (string.IsNullOrEmpty(segment)) continue;
                switch (EdiHelper.GetSegmentName(segment, _interchangeContext))
                {
                    //case EdiSegments.Una:
                    //    break;
                    case EdiSegments.Unb:
                        //    //InterchangeHeader = SegmentParser.ParseLine<S_UNB>(segment, _interchangeContext).Deserialize<S_UNB>();
                        //    break;
                        //case EdiSegments.Unz:
                        //case EdiSegments.Iea:
                        //    break;
                        //case EdiSegments.Ung:
                        //    //InterchangeGroup = SegmentParser.ParseLine<S_UNG>(segment, _interchangeContext).Deserialize<S_UNG>();
                        //    break;
                        //case EdiSegments.Gs:
                        //    //InterchangeGroup = SegmentParser.ParseLine<S_GS>(segment, _interchangeContext).Deserialize<S_GS>();
                        //    _envelope.Add(segment);
                        //    break;
                        //case EdiSegments.Une:
                        //case EdiSegments.Ge:
                        //    _envelope.Clear();
                        //    break;
                        //case EdiSegments.Unh:
                        //case EdiSegments.St:
                        message.Add(segment);
                        _envelope.Add(segment);
                        break;
                    case EdiSegments.Unz:
                        //case EdiSegments.Se:
                        message.Add(segment);
                        //Message = MessageLexer.Analyze(message, _envelope, _interchangeContext);
                        Message = message;
                        result = true;
                        // Once the message is parsed - it's removed
                        _envelope.Remove(_envelope.Last());
                        //message.Clear();
                        break;
                    default:
                        message.Add(segment);
                        break;
                }
            }
            return result;
        }

        public void Dispose()
        {
            _streamReader?.Dispose();
        }
    }
}