namespace Vegetarian.Domain.Models
{
    public class RefreshToken
    {
        public Guid Id { get; set; }
        public User User { get; set; } = null!;
        public Guid UserId { get; set; }

        public string Token { get; set; }
        public string JwtId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpriedAt { get; set; }
        public bool IsRevoked { get; set; }
    }
}
