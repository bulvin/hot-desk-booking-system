using Application.Interfaces;

namespace Infrastructure.Time;

public class DateTimeProvider : IDateTimeProvider
{
    private DateTime? _utcNow;

    public DateTime UtcNow
    {
        get => _utcNow ?? DateTime.UtcNow;
        set => _utcNow = value;
    }
}
