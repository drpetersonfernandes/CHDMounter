namespace SimpleChdDrive.Core.Tests.CHD.Utils;

public class ArrayPoolTests
{
    [Fact]
    public void Rent_ReturnsArrayOfCorrectSize()
    {
        var pool = new ArrayPool(1024);
        var arr = pool.Rent();
        Assert.Equal(1024, arr.Length);
    }

    [Fact]
    public void Rent_MultipleCalls_ReturnDifferentArrays()
    {
        var pool = new ArrayPool(256);
        var arr1 = pool.Rent();
        var arr2 = pool.Rent();
        Assert.NotSame(arr1, arr2);
    }

    [Fact]
    public void Return_And_Rent_ReturnsSameArray()
    {
        var pool = new ArrayPool(128);
        var arr1 = pool.Rent();
        arr1[0] = 42;
        pool.Return(arr1);
        var arr2 = pool.Rent();
        Assert.Same(arr1, arr2);
        Assert.Equal(42, arr2[0]);
    }

    [Fact]
    public void ReadStats_TracksIssuedAndReturned()
    {
        var pool = new ArrayPool(64);
        var arr1 = pool.Rent();
        var arr2 = pool.Rent();
        var arr3 = pool.Rent();

        pool.Return(arr1);
        pool.Return(arr2);

        pool.ReadStats(out var issued, out var returned);
        Assert.Equal(3, issued);
        Assert.Equal(2, returned);
    }

    [Fact]
    public void Rent_AllocatedNewWhenPoolEmpty()
    {
        var pool = new ArrayPool(32);
        var arr = pool.Rent();
        arr[0] = 0xFF;
        pool.Return(arr);
        var reused = pool.Rent();
        Assert.Equal(0xFF, reused[0]);
        var fresh = pool.Rent();
        Assert.NotSame(reused, fresh);
        Assert.Equal(0, fresh[0]);
    }

    [Fact]
    public void Return_MultipleArrays_RentsInLifoOrder()
    {
        var pool = new ArrayPool(100);
        var arr1 = pool.Rent();
        var arr2 = pool.Rent();

        arr1[0] = 1;
        arr2[0] = 2;

        pool.Return(arr1);
        pool.Return(arr2);

        var rented1 = pool.Rent();
        Assert.Equal(2, rented1[0]);

        var rented2 = pool.Rent();
        Assert.Equal(1, rented2[0]);
    }
}
