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
        Assert.That(SH.ParseToByteCount("03"), Is.EqualTo(3));
        Assert.That(SH.ParseToByteCount("03b"), Is.EqualTo(3));
        Assert.That(SH.ParseToByteCount("03B"), Is.EqualTo(3));
        Assert.That(SH.ParseToByteCount("0_3"), Is.EqualTo(3));
        Assert.That(SH.ParseToByteCount("0_3b"), Is.EqualTo(3));
        Assert.That(SH.ParseToByteCount("0_3B"), Is.EqualTo(3));
        Assert.That(SH.ParseToByteCount("0,3"), Is.EqualTo(3));
        Assert.That(SH.ParseToByteCount("0,3b"), Is.EqualTo(3));
        Assert.That(SH.ParseToByteCount("0,3B"), Is.EqualTo(3));
        
        
        Assert.That(SH.ParseToByteCount("31"), Is.EqualTo(31));
        Assert.That(SH.ParseToByteCount("31b"), Is.EqualTo(31));
        Assert.That(SH.ParseToByteCount("31B"), Is.EqualTo(31));
        Assert.Throws<ArgumentException>(() => SH.ParseToByteCount("31bi"));
        Assert.Throws<ArgumentException>(() => SH.ParseToByteCount("31Bi"));
        
        
        Assert.That(SH.ParseToByteCount("31k"), Is.EqualTo(31_744));
        Assert.That(SH.ParseToByteCount("31K"), Is.EqualTo(31_744));
        Assert.That(SH.ParseToByteCount("31ki"), Is.EqualTo(31_000));
        Assert.That(SH.ParseToByteCount("31Ki"), Is.EqualTo(31_000));
        
        
        Assert.That(SH.ParseToByteCount("314m"), Is.EqualTo(329_252_864));
        Assert.That(SH.ParseToByteCount("314M"), Is.EqualTo(329_252_864));
        Assert.That(SH.ParseToByteCount("314mi"), Is.EqualTo(314_000_000));
        Assert.That(SH.ParseToByteCount("314Mi"), Is.EqualTo(314_000_000));
        
        Assert.That(SH.ParseToByteCount("345g"), Is.EqualTo(370_440_929_280));
        Assert.That(SH.ParseToByteCount("345G"), Is.EqualTo(370_440_929_280));
        Assert.That(SH.ParseToByteCount("345gi"), Is.EqualTo(345_000_000_000));
        Assert.That(SH.ParseToByteCount("345Gi"), Is.EqualTo(345_000_000_000));
        
        Assert.That(SH.ParseToByteCount("64t"), Is.EqualTo(70_368_744_177_664));
        Assert.That(SH.ParseToByteCount("64T"), Is.EqualTo(70_368_744_177_664));
        Assert.That(SH.ParseToByteCount("64ti"), Is.EqualTo(64_000_000_000_000));
        Assert.That(SH.ParseToByteCount("64Ti"), Is.EqualTo(64_000_000_000_000));
    }
    [Test]
    public void ParseToByteCount_Invalids()
    {
        Assert.That(SH.ParseToByteCount(""), Is.Null);
        Assert.That(SH.ParseToByteCount("\t\t\t"), Is.Null);
        Assert.That(SH.ParseToByteCount(null), Is.Null);
        Assert.Throws<ArgumentException>(() => SH.ParseToByteCount("3bi"));
        Assert.Throws<ArgumentException>(() => SH.ParseToByteCount("3Bi"));
        Assert.Throws<ArgumentException>(() => SH.ParseToByteCount("03bi"));
        Assert.Throws<ArgumentException>(() => SH.ParseToByteCount("03Bi"));
        Assert.Throws<ArgumentException>(() => SH.ParseToByteCount("0_3bi"));
        Assert.Throws<ArgumentException>(() => SH.ParseToByteCount("0_3Bi"));
        Assert.Throws<ArgumentException>(() => SH.ParseToByteCount("0,3bi"));
        Assert.Throws<ArgumentException>(() => SH.ParseToByteCount("0,3Bi"));
        Assert.Throws<ArgumentException>(() => SH.ParseToByteCount("lsakjhfdskahf"));
        Assert.Throws<ArgumentException>(() => SH.ParseToByteCount("00EiB"));
    }

    [Test]
    public void ParseByteCount_WithSpaces()
    {
        Assert.That(SH.ParseToByteCount("64\tt"), Is.EqualTo(70_368_744_177_664));
        Assert.That(SH.ParseToByteCount("64\tT"), Is.EqualTo(70_368_744_177_664));
        Assert.That(SH.ParseToByteCount("64\tti"), Is.EqualTo(64_000_000_000_000));
        Assert.That(SH.ParseToByteCount("64\tTi"), Is.EqualTo(64_000_000_000_000));
        Assert.That(SH.ParseToByteCount("64\t  \tt"), Is.EqualTo(70_368_744_177_664));
        Assert.That(SH.ParseToByteCount("64\t  \tT"), Is.EqualTo(70_368_744_177_664));
        Assert.That(SH.ParseToByteCount("64\t  \tti"), Is.EqualTo(64_000_000_000_000));
        Assert.That(SH.ParseToByteCount("64\t  \tTi"), Is.EqualTo(64_000_000_000_000));
        Assert.That(SH.ParseToByteCount("64   t"), Is.EqualTo(70_368_744_177_664));
        Assert.That(SH.ParseToByteCount("64   T"), Is.EqualTo(70_368_744_177_664));
        Assert.That(SH.ParseToByteCount("64   ti"), Is.EqualTo(64_000_000_000_000));
        Assert.That(SH.ParseToByteCount("64   Ti"), Is.EqualTo(64_000_000_000_000));
    }
    
    [Test]
    public void ParseToByteCount_Decimal()
    {
        Assert.That(SH.ParseToByteCount("3.0000b"), Is.EqualTo(3));
        Assert.That(SH.ParseToByteCount("3.0001b"), Is.EqualTo(4));
        Assert.That(SH.ParseToByteCount("3.9001b"), Is.EqualTo(4));
        
        // kb -> b
        Assert.That(SH.ParseToByteCount("1,00.9001b"), Is.EqualTo(101));
        Assert.That(SH.ParseToByteCount("1,00.9001k"), Is.EqualTo(103_322));
        Assert.That(SH.ParseToByteCount("1,00.9001kb"), Is.EqualTo(103_322));
        // kib -> b
        Assert.That(SH.ParseToByteCount("1,00.9001ki"), Is.EqualTo(100_901));
        Assert.That(SH.ParseToByteCount("1,00.9001kib"), Is.EqualTo(100_901));
        Assert.That(SH.ParseToByteCount("1_00.9001ki"), Is.EqualTo(100_901));
        Assert.That(SH.ParseToByteCount("1_00.9001kib"), Is.EqualTo(100_901));
        
        // mb -> b
        Assert.That(SH.ParseToByteCount("1,00.9001m"), Is.EqualTo(105_801_424));
        Assert.That(SH.ParseToByteCount("1,00.9001mb"), Is.EqualTo(105_801_424));
        // mib -> b
        Assert.That(SH.ParseToByteCount("1,00.9001mi"), Is.EqualTo(100_900_100));
        Assert.That(SH.ParseToByteCount("1,00.9001mib"), Is.EqualTo(100_900_100));
        Assert.That(SH.ParseToByteCount("1_00.9001mi"), Is.EqualTo(100_900_100));
        Assert.That(SH.ParseToByteCount("1_00.9001mib"), Is.EqualTo(100_900_100));
        
        // gb -> b
        Assert.That(SH.ParseToByteCount("1,00.9001g"), Is.EqualTo(108_340_657_416));
        Assert.That(SH.ParseToByteCount("1,00.9001gb"), Is.EqualTo(108_340_657_416));
        
        // gib -> b
        Assert.That(SH.ParseToByteCount("1,00.9001gi"), Is.EqualTo(100_900_100_000));
        Assert.That(SH.ParseToByteCount("1,00.9001gib"), Is.EqualTo(100_900_100_000));
        Assert.That(SH.ParseToByteCount("1_00.9001gi"), Is.EqualTo(100_900_100_000));
        Assert.That(SH.ParseToByteCount("1_00.9001gib"), Is.EqualTo(100_900_100_000));
    }
}