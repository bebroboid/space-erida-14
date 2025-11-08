using Content.Server.GhostKick;
using Content.Shared._Orion.ServerProtection.Chat;
using Content.Shared.Administration.Managers;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Orion.ServerProtection.Chat;

//
// License-Identifier: AGPL-3.0-or-later
//

public sealed class ChatProtectionSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly ISharedAdminManager _admin = default!;
    [Dependency] private readonly ISharedChatManager _chat = default!;
    [Dependency] private readonly GhostKickManager _ghostKickManager = default!;

    private ISawmill _log = default!;
    private readonly List<ChatProtectionListPrototype> _index = new();
    private readonly HashSet<string> _icWords = new();
    private readonly HashSet<string> _oocWords = new();
    private bool _enabled = false;
    private bool _cacheDone = false;

    public override void Initialize()
    {
        base.Initialize();

        _log = Logger.GetSawmill("serverprotection.chat_protection");
        _proto.PrototypesReloaded += OnPrototypesReloaded;
        _cfg.OnValueChanged(CCVars.ChatProtectionEnabled, SetEnabled, true);
    }

    private void SetEnabled(bool value)
    {
        _enabled = value;
    }

    private void CachePrototypes()
    {
        _index.Clear();
        _icWords.Clear();
        _oocWords.Clear();

        foreach (var proto in _proto.EnumeratePrototypes<ChatProtectionListPrototype>())
        {
            _index.Add(proto);

            switch (proto.ID) // Handled by "Prototypes/_Orion/chat_protection.yml"
            {
                case "IC_BannedWords":
                    foreach (var word in proto.Words)
                    {
                        _icWords.Add(word);
                    }

                    break;

                case "OOC_BannedWords":
                    foreach (var word in proto.Words)
                    {
                        _oocWords.Add(word);
                    }

                    break;
            }
        }

        _log.Info($"Кэшировано {_icWords.Count} IC и {_oocWords.Count} OOC запрещённых слов.");
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        CachePrototypes();
    }

    public bool CheckICMessage(string message, EntityUid player)
    {
        if (!_enabled || string.IsNullOrEmpty(message))
            return false;

        if (!TryGetSession(player, out var session))
            return false;

        if (session == null)
            return false;

        if (_admin.IsAdmin(player, true))
           return false;

        if (!_cacheDone) // Something like initalization for prototypes
            CachePrototypes();

        foreach (var word in _icWords)
        {
            if (!message.Contains(word, StringComparison.OrdinalIgnoreCase))
                continue;

            HandleViolation(session, word, "IC");
            return true;
        }

        _cacheDone = true;

        return false;
    }

    public bool CheckOOCMessage(string message, ICommonSession session)
    {
        if (!_enabled || string.IsNullOrEmpty(message))
            return false;

        if (_admin.IsAdmin(session, true))
            return false;

        if (!_cacheDone) // Something like initalization for prototypes
            CachePrototypes();

        foreach (var word in _oocWords)
        {
            if (message.Contains(word, StringComparison.OrdinalIgnoreCase))
            {
                HandleViolation(session, word, "OOC");
                return true;
            }
        }

        _cacheDone = true;

        return false;
    }

    private bool TryGetSession(EntityUid player, out ICommonSession? session)
    {
        session = null;
        return _playerManager.TryGetSessionByEntity(player, out session);
    }

    private void HandleViolation(ICommonSession player, string word, string channel)
    {
        var reason = Loc.GetString("chat-protection-kick-reason", ("word", word), ("channel", channel)); // Erida-Edit | Ban > Kick
        _log.Info($"{player.Name} ({player.UserId}) использовал запрещённое слово: '{word}' в {channel}");

        switch (channel)
        {
            case "IC": // Kick them
            {
                // Erida-Edit-Start
                _chat.SendAdminAlert(Loc.GetString("chat-protection-admin-announcement-kick-reason", ("player", player.Name), ("word", word), ("channel", channel)));
                _ghostKickManager.DoDisconnect(player.Channel, reason);
                // Erida-Edit-End
                break;
            }

            case "OOC": // 把他放逐就行了。😡😡😡
            {
                // Erida-Edit-Start
                _chat.SendAdminAlert(Loc.GetString("chat-protection-admin-announcement-kick-reason", ("player", player.Name), ("word", word), ("channel", channel)));
                _ghostKickManager.DoDisconnect(player.Channel, reason);
                // Erida-Edit-End
                break;
            }
        }
    }
}
