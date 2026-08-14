namespace FrogSkill;

internal interface ITongueMode
{
    bool IsBusy { get; }
    bool CanRelease { get; }
    bool TryFire();
    void Release();
}
