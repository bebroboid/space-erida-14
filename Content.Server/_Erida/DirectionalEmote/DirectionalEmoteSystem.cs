using Content.Server.Chat.Managers;
using Content.Shared._Erida.DirectionalEmote;
using Content.Shared.Chat;
using Content.Shared.Examine;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._Erida.DirectionalEmote;

public sealed partial class DirectionalEmoteSystem : EntitySystem
{
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IGameTiming _gameTicking = default!;
    [Dependency] private readonly ExamineSystemShared _examineSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<SendDirectionalEmoteEvent>(OnSendDirectionalEmoteEvent);
    }

    private void OnSendDirectionalEmoteEvent(SendDirectionalEmoteEvent args)
    {
        if (!TryComp<ActorComponent>(GetEntity(args.Source), out var sourceActor) ||
            !TryComp<ActorComponent>(GetEntity(args.Target), out var targetActor) ||
            !TryComp<DirectionalEmoteTargetComponent>(GetEntity(args.Source), out var directEmote)) return;
        Logger.Debug("Отравбоа");
        var curTime = _gameTicking.CurTime;
        if (directEmote.LastSend + directEmote.Cooldown > curTime) return;

        var rangeError = Loc.GetString("directional-emote-range-error");
        if (!_examineSystem.InRangeUnOccluded(GetEntity(args.Source), GetEntity(args.Target), 10))
        {
            _chatManager.ChatMessageToOne(ChatChannel.Emotes, rangeError, rangeError, default, false, targetActor.PlayerSession.Channel);
            return;
        }

        var lengthError = Loc.GetString("directional-emote-length-error");
        if (args.Text.Length > 10000)
        {
            _chatManager.ChatMessageToOne(ChatChannel.Emotes, lengthError, lengthError, default, false, targetActor.PlayerSession.Channel);
            return;
        }


        _chatManager.ChatMessageToMany(ChatChannel.Emotes, args.Text, args.Text, GetEntity(args.Source), false, true, [targetActor.PlayerSession.Channel, sourceActor.PlayerSession.Channel]);
        directEmote.LastSend = curTime;
    }
}
