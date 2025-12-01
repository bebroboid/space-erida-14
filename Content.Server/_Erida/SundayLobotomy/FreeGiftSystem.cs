

using Content.Server.Hands.Systems;
using Content.Server.Jobs;
using Content.Shared.Hands.Components;
using Content.Shared.Humanoid;

public sealed partial class FreeGiftSystem : EntitySystem
{
    [Dependency] private readonly HandsSystem _handsSystem = default!;

    private float _nextGift = 0;
    private float _giftCooldown = 240f;


    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_nextGift > 0)
        {
            _nextGift -= frameTime;
            return;
        }

        var query = EntityQueryEnumerator<HandsComponent>();
        while (query.MoveNext(out var uid, out var hands))
        {
            var gift = Spawn("PresentRandomInsane");
            _handsSystem.PickupOrDrop(uid, gift);
        }
        _nextGift = _giftCooldown;
    }
}
