using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Io.Tests;

[TestClass]
public sealed class CsvTests
{
    [TestMethod]
    public void Read_ParsesRows()
    {
        var rows = Csv.Read(new StringReader("a,b\nc,d"));

        Assert.AreEqual(2, rows.Count);
        CollectionAssert.AreEqual(new[] { "a", "b" }, rows[0]);
        CollectionAssert.AreEqual(new[] { "c", "d" }, rows[1]);
    }

    [TestMethod]
    public void Read_SkipsBlankLines()
    {
        var rows = Csv.Read(new StringReader("a,b\n\n  \nc,d"));

        Assert.AreEqual(2, rows.Count);
    }

    [TestMethod]
    public void Write_WritesRows()
    {
        var writer = new StringWriter();

        Csv.Write(writer, new[]
        {
            new[] { "a", "b" },
            new[] { "c", "d" }
        });

        var expected = "a,b" + Environment.NewLine + "c,d" + Environment.NewLine;

        Assert.AreEqual(expected, writer.ToString());
    }

    [TestMethod]
    public void RoundTrip_PreservesRows()
    {
        var writer = new StringWriter();

        Csv.Write(writer, new[] { new[] { "a", "b" } });

        var rows = Csv.Read(new StringReader(writer.ToString()));

        CollectionAssert.AreEqual(new[] { "a", "b" }, rows[0]);
    }
}

