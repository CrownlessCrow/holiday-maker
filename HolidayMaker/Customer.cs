// En kund, samma kolumner som tabellen Customer. Email får vara null.
public sealed record Customer(int Id, string FirstName, string LastName, string Phone, string? Email);