using System.Text.Json.Serialization;

namespace System
{
    public readonly struct DateTimeRange
    {
        public static readonly DateTimeRange Anytime = new DateTimeRange(DateTime.MinValue, DateTime.MaxValue);
        public static readonly DateTimeRange Never = new DateTimeRange(DateTime.MinValue, DateTime.MinValue);

        public DateTime Start { get;  }
        public DateTime Finish { get; }
        [JsonIgnore]
        public TimeSpan Duration => Finish - Start; 

        public bool IsWithin(DateTimeRange range)
            => (Start >= range.Start) && (Finish <= range.Finish);

        public bool Intersects(DateTimeRange range)
            => (Start <= range.Finish) && (Finish >= range.Start);

        public bool Intersects(DateTime date)
            => (Start <= date) && (Finish >= date);

        public static IEnumerable<DateTimeRange> Union(ICollection<DateTimeRange> ranges)
        {
            if (ranges.Count > 0)
            {
                ranges = ranges.OrderBy(r => r.Start).ToList();
                var minStart = ranges.First().Start;
                var maxFinish = ranges.First().Finish;

                DateTime? lastFinish = null;

                foreach (var r in ranges.Skip(1))
                {
                    if (r.Start > maxFinish)
                    {
                        lastFinish = maxFinish;
                        yield return new DateTimeRange(minStart, maxFinish);

                        minStart = r.Start;
                        maxFinish = r.Finish;
                    }
                    else
                    {
                        if (r.Finish > maxFinish)
                            maxFinish = r.Finish;
                    }
                }

                if(!lastFinish.HasValue || lastFinish != maxFinish)
                    yield return new DateTimeRange(minStart, maxFinish);
            }
        }

        public IEnumerable<DateTimeRange> Inverse()
        {
            // The opposite of all time is never...
            if (Start == DateTime.MinValue && Finish == DateTime.MaxValue)
                yield break;

            if (Start > DateTime.MinValue)
                yield return new DateTimeRange(DateTime.MinValue, Start - DateTime.MinValue);

            if (Finish < DateTime.MaxValue)
                yield return new DateTimeRange(Finish, DateTime.MaxValue);
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
            var rangeFinish = r.Finish;
            if (rangeStart < Start)
                rangeStart = Start;

            if (rangeFinish > Finish)
                rangeFinish = Finish;

            if (rangeFinish >= Start && rangeStart <= Finish
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
            Finish = finish;
        }

        #region Overrides & Impliments
        public override string ToString() => ToString(true);
        public string ToString(bool includeTime)
        {
            if (Never == this) return "Never";
            else if (includeTime) return $"{Start} - {Finish} [{Duration}]";
            else return $"{Start:d} - {Finish:d} [{Duration.TotalDays:0}]";
        }

        public override bool Equals(object? obj) => (obj is DateTimeRange range) && range == this;
        public override int GetHashCode() => Start.GetHashCode() + Finish.GetHashCode();

        public static bool operator ==(DateTimeRange v1, DateTimeRange v2) => (v1.Start == v2.Start) && (v1.Finish == v2.Finish);
        public static bool operator !=(DateTimeRange v1, DateTimeRange v2) => (v1.Start != v2.Start) || (v1.Finish != v2.Finish);
        #endregion
    }
}
