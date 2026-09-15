namespace TigerGateDemo.Core.Entities;

public class Customer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public DateTimeOffset RegisteredAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<Order> Orders { get; set; } = new List<Order>();

    public bool IsBusinessAccount => !string.IsNullOrWhiteSpace(CompanyName);
}
