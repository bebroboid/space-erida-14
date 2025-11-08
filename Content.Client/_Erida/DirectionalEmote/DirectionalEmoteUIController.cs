using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client._Erida.DirectionalEmote;

[UsedImplicitly]
public sealed class DirectionalEmoteUIController : UIController
{
    [UISystemDependency] private readonly DirectionalEmoteSystem _directionalEmoteSystem = default!;
    private DirectionalEmoteWindow _emoteWindow = default!;

    public void OpenWindow()
    {
        EnsureWindow();

        _emoteWindow.OpenCentered();
        _emoteWindow.MoveToFront();

        _emoteWindow.AcceptPressed += () =>
        {
            _directionalEmoteSystem.ShowMessage(_emoteWindow.Source, _emoteWindow.Target, _emoteWindow.Text);
            _emoteWindow.Dispose();
        };
    }

    private void EnsureWindow()
    {
        if (_emoteWindow is { Disposed: false })
            return;

        _emoteWindow = UIManager.CreateWindow<DirectionalEmoteWindow>();
    }

    public void ToggleWindow(NetEntity source, NetEntity emoteTarget)
    {
        EnsureWindow();
        _emoteWindow.Source = source;
        _emoteWindow.Target = emoteTarget;

        if (_emoteWindow.IsOpen)
        {
            _emoteWindow.Dispose();
        }
        else
        {
            OpenWindow();
        }
    }
}
