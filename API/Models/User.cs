namespace API.Models;

// Simple user entity. In real systems add validation & constraints.
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
}