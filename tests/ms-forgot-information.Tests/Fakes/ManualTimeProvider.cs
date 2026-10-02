namespace ms_forgot_information.Tests.Fakes;

/// <summary>Lets tests fast-forward past OTP expiration without real delays.</summary>
public class ManualTimeProvider : TimeProvider
{
    private DateTimeOffset _now = DateTimeOffset.UtcNow;

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan delta) => _now = _now.Add(delta);
}
