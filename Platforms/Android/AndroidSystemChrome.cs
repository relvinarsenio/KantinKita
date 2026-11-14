using KantinKita.Services;

namespace KantinKita.Platforms;

internal sealed class AndroidSystemChrome : ISystemChrome
{
    public void Apply() => SystemUi.Apply();
}

