namespace GameOfLife.Domain.Tests;

[TestFixture]
public sealed class LifeRulesTests
{
    [TestCase(0, false, TestName = "Live cell with 0 neighbours dies (underpopulation)")]
    [TestCase(1, false, TestName = "Live cell with 1 neighbour dies (underpopulation)")]
    [TestCase(2, true, TestName = "Live cell with 2 neighbours survives")]
    [TestCase(3, true, TestName = "Live cell with 3 neighbours survives")]
    [TestCase(4, false, TestName = "Live cell with 4 neighbours dies (overpopulation)")]
    [TestCase(8, false, TestName = "Live cell with 8 neighbours dies (overpopulation)")]
    public void Conway_LiveCell(int neighbours, bool expected) =>
        Assert.That(LifeRules.Conway.IsAliveInNextGeneration(isAlive: true, neighbours), Is.EqualTo(expected));

    [TestCase(2, false)]
    [TestCase(3, true, TestName = "Dead cell with 3 neighbours is born (reproduction)")]
    [TestCase(4, false)]
    [TestCase(0, false)]
    public void Conway_DeadCell(int neighbours, bool expected) =>
        Assert.That(LifeRules.Conway.IsAliveInNextGeneration(isAlive: false, neighbours), Is.EqualTo(expected));

    [TestCase(-1)]
    [TestCase(9)]
    public void IsAliveInNextGeneration_RejectsImpossibleNeighbourCounts(int neighbours) =>
        Assert.That(() => LifeRules.Conway.IsAliveInNextGeneration(true, neighbours), Throws.InstanceOf<ArgumentOutOfRangeException>());

    [Test]
    public void Constructor_RejectsOutOfRangeCounts() =>
        Assert.That(() => new LifeRules([9], [2]), Throws.InstanceOf<ArgumentOutOfRangeException>());

    [Test]
    public void CustomRules_AreApplied()
    {
        var highLife = new LifeRules([3, 6], [2, 3]); // B36/S23

        Assert.That(highLife.IsAliveInNextGeneration(isAlive: false, 6), Is.True);
    }
}
