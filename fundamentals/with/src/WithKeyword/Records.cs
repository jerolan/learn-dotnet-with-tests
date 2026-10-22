namespace WithKeyword;

public record Person(string Name, int Age);

public record Address(string City);

public record Customer(string Name, Address Home);

