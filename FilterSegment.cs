namespace EdifactFramework
{
    public class FilterSegment
    {
        private string Segment { get; set; }

        private string PreviousSegment { get; set; }

        public FilterSegment(string segment, string previousSegment)
        {
            Segment = segment;
            PreviousSegment = previousSegment;
        }
    }
}