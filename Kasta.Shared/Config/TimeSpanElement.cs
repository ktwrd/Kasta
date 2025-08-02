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
        if (Ticks is not null)
        {
            return new TimeSpan(Ticks.Value);
        }
        var d = Days.GetValueOrDefault(0);
        var h = Hours.GetValueOrDefault(0);
        var min = Minutes.GetValueOrDefault(0);
        var s = Seconds.GetValueOrDefault(0);
        var ms = Milliseconds.GetValueOrDefault(0);
        var mics = Microseconds.GetValueOrDefault(0);
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