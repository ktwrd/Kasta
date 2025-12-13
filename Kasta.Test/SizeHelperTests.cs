using SH = Kasta.Shared.Helpers.SizeHelper;

namespace Kasta.Test;

public class Tests
{
    [Test]
    public void ParseToByteCount()
    {
        Assert.That(SH.ParseToByteCount("3"), Is.EqualTo(3));
        Assert.That(SH.ParseToByteCount("3b"), Is.EqualTo(3));
        Assert.That(SH.ParseToByteCount("3B"), Is.EqualTo(3));
        Assert.That(SH.ParseToByteCount("3bi"), Is.EqualTo(0));
        Assert.That(SH.ParseToByteCount("3Bi"), Is.EqualTo(0));
        
        Assert.That(SH.ParseToByteCount("3.0000b"), Is.EqualTo(3));
        Assert.That(SH.ParseToByteCount("3.0001b"), Is.EqualTo(3));
        Assert.That(SH.ParseToByteCount("3.9001b"), Is.EqualTo(4));
        
        
        Assert.That(SH.ParseToByteCount("1,00.9001b"), Is.EqualTo(101));
        Assert.That(SH.ParseToByteCount("1,00.9001k"), Is.EqualTo(103_322));
        Assert.That(SH.ParseToByteCount("1,00.9001kb"), Is.EqualTo(103_322));
        
        Assert.That(SH.ParseToByteCount("1,00.9001ki"), Is.EqualTo(100_900));
        Assert.That(SH.ParseToByteCount("1,00.9001kib"), Is.EqualTo(100_900));
        Assert.That(SH.ParseToByteCount("1_00.9001ki"), Is.EqualTo(100_900));
        Assert.That(SH.ParseToByteCount("1_00.9001kib"), Is.EqualTo(100_900));
    }
}