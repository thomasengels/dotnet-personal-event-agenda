namespace Event.Domain.Tests;

public class EntityTests
{
    private sealed class TestEntityA(Guid id) : Entity<Guid>(id);

    private sealed class TestEntityB(Guid id) : Entity<Guid>(id);

    [Fact]
    public void Equals_SameIdSameType_ReturnsTrue()
    {
        var id = Guid.NewGuid();

        Assert.Equal(new TestEntityA(id), new TestEntityA(id));
    }

    [Fact]
    public void GetHashCode_SameIdSameType_ReturnsSameValue()
    {
        var id = Guid.NewGuid();

        Assert.Equal(new TestEntityA(id).GetHashCode(), new TestEntityA(id).GetHashCode());
    }

    [Fact]
    public void Equals_DifferentIds_ReturnsFalse()
    {
        Assert.NotEqual(new TestEntityA(Guid.NewGuid()), new TestEntityA(Guid.NewGuid()));
    }

    [Fact]
    public void Equals_SameIdDifferentType_ReturnsFalse()
    {
        var id = Guid.NewGuid();

        Assert.False(new TestEntityA(id).Equals(new TestEntityB(id)));
    }

    [Fact]
    public void Equals_SameReference_ReturnsTrue()
    {
        var entity = new TestEntityA(Guid.NewGuid());

        Assert.Equal(entity, entity);
    }

    [Fact]
    public void Equals_Null_ReturnsFalse()
    {
        Assert.False(new TestEntityA(Guid.NewGuid()).Equals(null));
    }
}
