using CodeBeam.UltimateAuth.Core.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using System.Text.Json;

namespace CodeBeam.UltimateAuth.Client;

public abstract class UAuthFlowPageBase : UAuthReactiveComponentBase
{
    [Inject] protected NavigationManager Nav { get; set; } = default!;

    protected AuthFlowPayload? UAuthPayload { get; private set; }
    protected string? ReturnUrl { get; private set; }
    protected bool ShouldFocus { get; private set; }
    protected string? Identifier { get; private set; }


    protected virtual bool ClearQueryAfterParse => true;

    private bool _needsClear;
    private string? _lastParsedUri;
    private bool _payloadConsumed;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        var currentUri = Nav.Uri;

        if (string.Equals(_lastParsedUri, currentUri, StringComparison.Ordinal))
            return;

        _lastParsedUri = currentUri;

        _payloadConsumed = false;

        var uri = Nav.ToAbsoluteUri(currentUri);
        var query = QueryHelpers.ParseQuery(uri.Query);

        ShouldFocus = query.TryGetValue("focus", out var focus) && focus == "1";
        ReturnUrl = query.TryGetValue("returnUrl", out var ru) ? ru.ToString() : null;
        Identifier = query.TryGetValue("identifier", out var id) ? id.ToString() : null;

        UAuthPayload = null;

        if (query.TryGetValue("uauth", out var raw) && !string.IsNullOrWhiteSpace(raw))
        {
            try
            {
                var bytes = WebEncoders.Base64UrlDecode(raw!);
                var json = Encoding.UTF8.GetString(bytes);
                UAuthPayload = JsonSerializer.Deserialize<AuthFlowPayload>(json);
            }
            catch
            {
                UAuthPayload = null;
            }
        }

        _needsClear = ClearQueryAfterParse && uri.Query.Length > 1;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (TryConsumePayload(out var payload))
            await OnUAuthPayloadAsync(payload!);

        if (ConsumeFocus())
            await OnFocusRequestedAsync();

        if (_needsClear)
        {
            _needsClear = false;
            var clean = new Uri(Nav.Uri).GetLeftPart(UriPartial.Path);
            Nav.NavigateTo(clean, replace: true);
        }
    }

    protected bool ConsumeFocus()
    {
        if (!ShouldFocus)
            return false;

        ShouldFocus = false;
        return true;
    }

    protected bool TryConsumePayload(out AuthFlowPayload? payload)
    {
        if (_payloadConsumed || UAuthPayload is null)
        {
            payload = null;
            return false;
        }

        _payloadConsumed = true;
        payload = UAuthPayload;
        return true;
    }

    protected virtual Task OnUAuthPayloadAsync(AuthFlowPayload payload) => Task.CompletedTask;
    protected virtual Task OnFocusRequestedAsync() => Task.CompletedTask;
}