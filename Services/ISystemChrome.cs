namespace KantinKita.Services;

/// <summary>Keeps the Android status/navigation bar colours in sync with the app theme.</summary>
public interface ISystemChrome
{
    void Apply();
}
