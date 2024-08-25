using System.Text.Json.Serialization;

namespace System
{
    public readonly struct DateTimeRange : IEquatable<DateTimeRange>
    {
        public static readonly DateTimeRange Anytime = new DateTimeRange(DateTime.MinValue, DateTime.MaxValue);
        public static readonly DateTimeRange Never = new DateTimeRange(DateTime.MinValue, DateTime.MinValue);

        public DateTime Start { get; }
        public DateTime End { get; }
        [JsonIgnore]
        public TimeSpan Duration => End - Start;

        public bool Contains(DateTime date) => (Start <= date) && (End >= date);
        public bool Contains(DateTimeRange range) => (Start <= range.Start) && (End >= range.End);
        public bool Intersects(DateTimeRange range) => (Start <= range.End) && (End >= range.Start);
        
        public static IEnumerable<DateTimeRange> Union(ICollection<DateTimeRange> ranges)
        {
            if (ranges.Count == 0) yield break;

            var sortedRanges = ranges.OrderBy(r => r.Start).ToList();
            var current = sortedRanges[0];

            foreach (var range in sortedRanges.Skip(1))
            {
                if (range.Start > current.End)
                {
                    yield return current;
                    current = range;
                }
                else if (range.End > current.End)
                {
                    current = new DateTimeRange(current.Start, range.End);
                }
            }

            yield return current;
        }

        public IEnumerable<DateTimeRange> Inverse()
        {
            if (this == Anytime)
                yield break;

            if (Start > DateTime.MinValue)
                yield return new DateTimeRange(DateTime.MinValue, Start);

            if (End < DateTime.MaxValue)
                yield return new DateTimeRange(End, DateTime.MaxValue);
        }

        public IEnumerable<DateTimeRange> Intersect(IEnumerable<DateTimeRange> ranges)
        {
            return ranges.Select(Intersect).Where(r => r != Never);
        }

        public DateTimeRange Intersect(DateTimeRange range)
        {
            var start = (range.Start < Start) ? Start : range.Start;
            var end = (range.End > End) ? End : range.End;

            return start < end ? new DateTimeRange(start, end) : Never;
        }

        public DateTimeRange(DateTime start, TimeSpan timeSpan) : this(start, start + timeSpan) { }
        public DateTimeRange(DateTime start, DateTime end)
        {
            if (end < start)
                throw new ArgumentOutOfRangeException(nameof(end), end, "The end date cannot be before the start date.");

            Start = start;
            End = end;
        }

        #region Overrides & Impliments
        public override string ToString() => ToString(true);
        public string ToString(bool includeTime)
        {
            if (Never == this) return "Never";
            else if (Anytime == this) return "Anytime";

            return includeTime
                 ? $"{Start} - {End} [{Duration}]"
                 : $"{Start:d} - {End:d} [{Duration.TotalDays:0}]";
        }

        public bool Equals(DateTimeRange other) => Start == other.Start && End == other.End;
        public override bool Equals(object? obj) => (obj is DateTimeRange other) && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Start, End);

        public static bool operator ==(DateTimeRange left, DateTimeRange right) => left.Equals(right);
        public static bool operator !=(DateTimeRange left, DateTimeRange right) => !left.Equals(right);
        #endregion
    }
}
