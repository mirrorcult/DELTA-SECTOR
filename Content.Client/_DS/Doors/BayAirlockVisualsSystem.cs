using Content.Shared.Doors.Components;
using Robust.Client.Animations;
using Robust.Client.GameObjects;

namespace Content.Client._DS.Doors;

public sealed class BayAirlockVisualsSystem : VisualizerSystem<BayAirlockVisualsComponent>
{
    private const string AnimationKey = "airlock_animation";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BayAirlockVisualsComponent, ComponentInit>(OnInit);
    }

    private void OnInit(EntityUid uid, BayAirlockVisualsComponent component, ComponentInit args)
    {
        component.CloseAnimation = new Animation {Length = TimeSpan.FromSeconds(component.Delay)};
        {
            foreach (var val in Enum.GetValues<BayDoorAnimatedLayers>())
            {
                var flick = new AnimationTrackSpriteFlick();
                component.CloseAnimation.AnimationTracks.Add(flick);
                flick.LayerKey = val;
                flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("closing", 0f));
            }

            var unlitFlick = new AnimationTrackSpriteFlick();
            component.CloseAnimation.AnimationTracks.Add(unlitFlick);
            unlitFlick.LayerKey = BayDoorAncillaryLayers.GreenLights;
            unlitFlick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("closing", 0f));
        }

        component.OpenAnimation = new Animation {Length = TimeSpan.FromSeconds(component.Delay)};
        {
            foreach (var val in Enum.GetValues<BayDoorAnimatedLayers>())
            {
                var flick = new AnimationTrackSpriteFlick();
                component.OpenAnimation.AnimationTracks.Add(flick);
                flick.LayerKey = val;
                flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("opening", 0f));
            }

            var unlitFlick = new AnimationTrackSpriteFlick();
            component.OpenAnimation.AnimationTracks.Add(unlitFlick);
            unlitFlick.LayerKey = BayDoorAncillaryLayers.GreenLights;
            unlitFlick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("opening", 0f));
        }

        component.DenyAnimation = new Animation {Length = TimeSpan.FromSeconds(component.Delay)};
        {
            var flick = new AnimationTrackSpriteFlick();
            component.DenyAnimation.AnimationTracks.Add(flick);
            flick.LayerKey = BayDoorAncillaryLayers.DenyLights;
            flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("deny", 0f));
        }

        component.EmagAnimation = new Animation {Length = TimeSpan.FromSeconds(component.Delay)};
        {
            var flick = new AnimationTrackSpriteFlick();
            component.EmagAnimation.AnimationTracks.Add(flick);
            flick.LayerKey = BayDoorAncillaryLayers.Emag;
            flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame("closed", 0f));
        }

        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        EnsureComp<AnimationPlayerComponent>(uid);

        if (component.DoorColor != null)
        {
            if (sprite.LayerMapTryGet(BayDoorAnimatedLayers.Color, out var colorLayer))
            {
                sprite.LayerSetColor(colorLayer, component.DoorColor.Value);
            }
            if (sprite.LayerMapTryGet(BayDoorAnimatedLayers.ColorFill, out var colorFillLayer))
            {
                sprite.LayerSetColor(colorFillLayer, component.DoorColor.Value);
            }
        }
        if (component.StripeColor != null)
        {
            if (sprite.LayerMapTryGet(BayDoorAnimatedLayers.Stripe, out var stripeLayer))
            {
                sprite.LayerSetColor(stripeLayer, component.StripeColor.Value);
            }
            if (sprite.LayerMapTryGet(BayDoorAnimatedLayers.StripeFill, out var stripeFillLayer))
            {
                sprite.LayerSetColor(stripeFillLayer, component.StripeColor.Value);
            }
        }
    }

    protected override void OnAppearanceChange(EntityUid uid, BayAirlockVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite is not { } sprite)
            return;

        if (!TryComp<AnimationPlayerComponent>(uid, out var animPlayer))
            return;

        if (!args.Component.TryGetData(DoorVisuals.State, out DoorState state))
        {
            state = DoorState.Closed;
        }

        if (animPlayer.HasRunningAnimation(AnimationKey))
        {
            animPlayer.Stop(AnimationKey);
        }

        void SetAnimatedTo(string st)
        {
            foreach (var val in Enum.GetValues<BayDoorAnimatedLayers>())
            {
                if (sprite.LayerMapTryGet(val, out var lay))
                {
                    sprite.LayerSetState(lay, st);
                }
            }
        }


        args.Component.TryGetData(DoorVisuals.Powered, out bool powered);

        var welded = false;
        var boltedVisible = args.Component.TryGetData(DoorVisuals.BoltLights, out bool lights) && lights && powered;
        var denyVisible = false;
        var greenVisible = false;
        var emagVisible = false;

        switch (state)
        {
            case DoorState.Open:
                SetAnimatedTo("open");
                break;
            case DoorState.Closed:
                SetAnimatedTo("closed");
                break;
            case DoorState.Opening:
                animPlayer.Play(component.OpenAnimation, AnimationKey);
                greenVisible = powered;
                break;
            case DoorState.Closing:
                animPlayer.Play(component.CloseAnimation, AnimationKey);
                greenVisible = powered;
                break;
            case DoorState.Denying:
                animPlayer.Play(component.DenyAnimation, AnimationKey);
                denyVisible = powered;
                break;
            case DoorState.Emagging:
                animPlayer.Play(component.EmagAnimation, AnimationKey);
                emagVisible = powered;
                break;
            case DoorState.Welded:
                welded = true;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (sprite.LayerMapTryGet(BayDoorAncillaryLayers.DenyLights, out var denyLayer))
        {
            sprite.LayerSetVisible(denyLayer, denyVisible);
        }

        if (sprite.LayerMapTryGet(BayDoorAncillaryLayers.GreenLights, out var greenLayer))
        {
            sprite.LayerSetVisible(greenLayer, greenVisible);
        }

        if (sprite.LayerMapTryGet(BayDoorAncillaryLayers.Emag, out var emagLayer))
        {
            sprite.LayerSetVisible(emagLayer, emagVisible);
        }

        if (sprite.LayerMapTryGet(BayDoorAncillaryLayers.BoltLights, out var boltsLayer))
        {
            sprite.LayerSetVisible(boltsLayer, boltedVisible);
        }

        if (sprite.LayerMapTryGet(BayDoorAncillaryLayers.Welded, out var layer))
        {
            sprite.LayerSetVisible(layer, welded);
        }
    }
}
