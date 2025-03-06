using System.Xml.Serialization;

namespace Kasta.Shared;

public class TimeSpanElement
{
    public static TimeSpanElement FromTimeSpan(TimeSpan src, bool ticksOnly = true)
    {
        if (ticksOnly)
        {
            return new TimeSpanElement()
            {
                Ticks = src.Ticks
            };
        }
        else
        {
            return new TimeSpanElement()
            {
                Days = src.Days,
                Hours = src.Hours,
                Minutes = src.Minutes,
                Seconds = src.Seconds,
                Milliseconds = src.Milliseconds,
                Microseconds = src.Microseconds
            };
        }
    }
    public static TimeSpanElement FromSeconds(int seconds)
    {
        return new TimeSpanElement()
        {
            Seconds = seconds
        };
    }
    [XmlElement("Ticks")]
    public long? Ticks { get; set; }

    [XmlElement("Days")]
    public int? Days { get; set; }

    [XmlElement("Hours")]
    public int? Hours { get; set; }

    [XmlElement("Minutes")]
    public int? Minutes { get; set; }

    [XmlElement("Seconds")]
    public int? Seconds { get; set; }

    [XmlElement("Milliseconds")]
    public int? Milliseconds { get; set; }

    [XmlElement("Microseconds")]
    public int? Microseconds { get; set; }

    public TimeSpan ToTimeSpan()
    {
        if (Ticks != null && Ticks.HasValue)
        {
            return new TimeSpan(Ticks.Value);
        }
        int d = Days == null ? 0 : Days.HasValue ? Days.Value : 0;
        int h = Hours == null ? 0 : Hours.HasValue ? Hours.Value : 0;
        int min = Minutes == null ? 0 : Minutes.HasValue ? Minutes.Value : 0;
        int s = Seconds == null ? 0 : Seconds.HasValue ? Seconds.Value : 0;
        int ms = Milliseconds == null ? 0 : Milliseconds.HasValue ? Milliseconds.Value : 0;
        int mics = Microseconds == null ? 0 : Microseconds.HasValue ? Microseconds.Value : 0;
        return new TimeSpan(
            d,
            h,
            min,
            s,
            ms,
            mics
        );
    }
}