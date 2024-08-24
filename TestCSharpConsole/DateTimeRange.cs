using System.Text.Json.Serialization;

namespace System
{
    public readonly struct DateTimeRange
    {
        public static readonly DateTimeRange Anytime = new DateTimeRange(DateTime.MinValue, DateTime.MaxValue);
        public static readonly DateTimeRange Never = new DateTimeRange(DateTime.MinValue, DateTime.MinValue);

        public DateTime Start { get;  }
        public DateTime End { get; }
        [JsonIgnore]
        public TimeSpan Duration => End - Start; 

        public bool IsWithin(DateTimeRange range)
            => (Start >= range.Start) && (End <= range.End);

        public bool Intersects(DateTimeRange range)
            => (Start <= range.End) && (End >= range.Start);

        public bool Intersects(DateTime date)
            => (Start <= date) && (End >= date);

        public static IEnumerable<DateTimeRange> Union(ICollection<DateTimeRange> ranges)
        {
            if (ranges.Count > 0)
            {
                ranges = ranges.OrderBy(r => r.Start).ToList();
                var minStart = ranges.First().Start;
                var maxFinish = ranges.First().End;

                DateTime? lastFinish = null;

                foreach (var r in ranges.Skip(1))
                {
                    if (r.Start > maxFinish)
                    {
                        lastFinish = maxFinish;
                        yield return new DateTimeRange(minStart, maxFinish);

                        minStart = r.Start;
                        maxFinish = r.End;
                    }
                    else
                    {
                        if (r.End > maxFinish)
                            maxFinish = r.End;
                    }
                }

                if(!lastFinish.HasValue || lastFinish != maxFinish)
                    yield return new DateTimeRange(minStart, maxFinish);
            }
        }

        public IEnumerable<DateTimeRange> Inverse()
        {
            // The opposite of all time is never...
            if (Start == DateTime.MinValue && End == DateTime.MaxValue)
                yield break;

            if (Start > DateTime.MinValue)
                yield return new DateTimeRange(DateTime.MinValue, Start - DateTime.MinValue);

            if (End < DateTime.MaxValue)
                yield return new DateTimeRange(End, DateTime.MaxValue);
        }

        public IEnumerable<DateTimeRange> Intersect(IEnumerable<DateTimeRange> ranges)
        {
            foreach (var r in ranges)
            {
                var newRange = Intersect(r);
                if(newRange != Never)
                    yield return newRange;
            }
        }

        public DateTimeRange Intersect(DateTimeRange r)
        {
            var rangeStart = r.Start;
            var rangeFinish = r.End;
            if (rangeStart < Start)
                rangeStart = Start;

            if (rangeFinish > End)
                rangeFinish = End;

            if (rangeFinish >= Start && rangeStart <= End
                && rangeStart != rangeFinish)
                return new DateTimeRange(rangeStart, rangeFinish);
            else
                return Never;
        }

        public DateTimeRange(DateTime start, TimeSpan timeSpan) : this(start, start + timeSpan) { }
        public DateTimeRange(DateTime start, DateTime finish)
        {
            if (finish < start)
                throw new ArgumentOutOfRangeException($"{nameof(finish)}", finish, "The finish date cannot be before the start date.");

            Start = start;
            End = finish;
        }

        #region Overrides & Impliments
        public override string ToString() => ToString(true);
        public string ToString(bool includeTime)
        {
            if (Never == this) return "Never";
            else if (includeTime) return $"{Start} - {End} [{Duration}]";
            else return $"{Start:d} - {End:d} [{Duration.TotalDays:0}]";
        }

        public override bool Equals(object? obj) => (obj is DateTimeRange range) && range == this;
        public override int GetHashCode() => Start.GetHashCode() + End.GetHashCode();

        public static bool operator ==(DateTimeRange v1, DateTimeRange v2) => (v1.Start == v2.Start) && (v1.End == v2.End);
        public static bool operator !=(DateTimeRange v1, DateTimeRange v2) => (v1.Start != v2.Start) || (v1.End != v2.End);
        #endregion
    }
}
