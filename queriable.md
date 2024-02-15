## IQueryable

`IQueryable` is a powerful interface in the .NET framework that allows for efficient, deferred execution of queries against a data source. Unlike `IEnumerable`, which executes queries in the client's memory, `IQueryable` executes queries against the data source, making it a better choice for remote data sources like databases. This chapter will explore how to use `IQueryable` to create composable and efficient queries. We'll apply Test-Driven Development (TDD) with MSTest to understand and validate the behavior of `IQueryable` operations.

### Understanding IQueryable

`IQueryable` extends `IEnumerable` and is part of the System.Linq namespace. It is designed to translate its queries into the domain-specific language of the data source, for example, SQL for a relational database, allowing the query to be executed by the data source itself.

### Setting Up Tests for IQueryable

Testing code that uses `IQueryable` often involves mocking or simulating a data source that can process expressions, such as a database context in Entity Framework.

1. **Navigate to Your Test Project**: Ensure you're within the `MyFirstDotNetApp.Tests` project directory.
2. **Create a Test Class for IQueryable**: Consider creating a test class named `IQueryableTests.cs` to contain your tests related to querying with `IQueryable`.

### Writing Tests for IQueryable

#### Installing Moq

Moq is a popular mocking library for .NET that allows you to create mock objects in unit tests, making it easier to test components in isolation. Here are the steps to add Moq to your project using the .NET CLI (Command Line Interface):

1. **Open a Terminal or Command Prompt**: Navigate to the root directory of your test project where the `.csproj` file is located.

2. **Add Moq Package**: Run the following command to add the Moq package to your project:

   ```
   dotnet add ./MyFirstDotNetApp.Tests/MyFirstDotNetApp.Tests.csproj package Moq
   ```

   This command adds the latest version of Moq to your project by updating your `.csproj` file and downloading the package.

#### Testing IQueryable Query Execution

Let's simulate a scenario where we use `IQueryable` to query a data source. In a real-world application, this could involve querying a database with Entity Framework, but for our tests, we'll mock the behavior.

```csharp
using System.Linq;
using Moq;
using System.Collections.Generic;

namespace MyFirstDotNetApp.Tests
{
    [TestClass]
    public class IQueryableTests
    {
        [TestMethod]
        public void IQueryable_QueryExecutedOnDataSource_ReturnsExpectedResults()
        {
            // Arrange
            var data = new List<Person> // Person already exist from the previous chapters
            {
                new Person { Name = "Alice", Age = 30 },
                new Person { Name = "Bob", Age = 25 },
                new Person { Name = "Charlie", Age = 35 }
            }.AsQueryable();

            var queryableData = new Mock<IQueryable<Person>>();
            queryableData.Setup(m => m.Provider).Returns(data.Provider);
            queryableData.Setup(m => m.Expression).Returns(data.Expression);
            queryableData.Setup(m => m.ElementType).Returns(data.ElementType);
            queryableData.Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());

            // Act
            var results = queryableData.Object.Where(p => p.Age > 30).ToList();

            // Assert
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("Charlie", results.First().Name);
        }
    }
}
```

### Understanding Deferred Execution

`IQueryable` queries are not executed when they are defined but rather when their results are iterated over. This characteristic is known as deferred execution, and it allows for query composition; additional conditions can be appended to a query before it is sent to the data source.

### Conclusion

This chapter introduced `IQueryable` and its role in constructing efficient and composable queries in .NET applications, particularly those that interact with databases. By leveraging `IQueryable`, applications can minimize the amount of data transferred from the data source and execute complex queries efficiently.

Testing `IQueryable` operations with MSTest, especially when combined with mocking frameworks like Moq, helps ensure that your queries behave as expected without requiring access to the actual data source. As you develop data-driven applications in .NET, becoming proficient with `IQueryable` will enable you to write more efficient, scalable, and maintainable data access code.
