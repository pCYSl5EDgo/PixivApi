using Xunit;

namespace PixivApi.Core.Test;

public class StringSetTest
{
    [Fact]
    public void NoLackTest()
    {
        var set = new StringSet(4);
        set.Register("1");
        set.Register("2");
        set.Register("3");
        set.Register("4");

        Assert.Empty(set.GetLackedNumbers());
    }

    private static void Add(StringSet set, uint number, uint diff = 0)
    {
        var text = (number + diff).ToString();
        set.Values.TryAdd(number, text);
        set.Reverses.TryAdd(text, number);
    }

    private static void Expect(IEnumerator<(uint lacked, uint value)> enumerator, uint lacked, uint value)
    {
        Assert.True(enumerator.MoveNext());
        var item = enumerator.Current;
        Assert.Equal(lacked, item.lacked);
        Assert.Equal(value, item.value);
    }

    [Fact]
    public void LackFirstTest()
    {
        var set = new StringSet(4);
        for (uint i = 0; i < 4; i++)
        {
            Add(set, i + 2);
        }

        using var e = set.GetLackedNumbers().GetEnumerator();
        Expect(e, 1, 5);
        Assert.False(e.MoveNext());
    }

    [Fact]
    public void LackMiddleTest()
    {
        var set = new StringSet(4);
        Add(set, 1);
        Add(set, 5);
        Add(set, 6);
        Add(set, 7);

        using var e = set.GetLackedNumbers().GetEnumerator();
        Expect(e, 2, 7);
        Expect(e, 3, 6);
        Expect(e, 4, 5);
        Assert.False(e.MoveNext());
    }

    [Fact]
    public void LackMiddleShortenTest()
    {
        var set = new StringSet(4);
        Add(set, 1);
        Add(set, 2);
        Add(set, 6);
        Add(set, 7);

        using var e = set.GetLackedNumbers().GetEnumerator();
        Expect(e, 3, 7);
        Expect(e, 4, 6);
        Assert.False(e.MoveNext());
    }
}
