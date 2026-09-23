using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using System.Text.Json;

namespace CodeBeam.UltimateAuth.Client.Blazor;

/// <summary>
/// Base class for UltimateAuth Blazor pages that participate in authentication flows and consume UltimateAuth flow parameters
/// from the current URL.
/// </summary>
public abstract class UAuthPageBase : UAuthComponentBase
{
    /// <summary>
    /// Gets the UltimateAuth flow payload parsed from the current URL, when one is available and valid.
    /// </summary>
    protected AuthFlowPayload? UAuthPayload { get; private set; }

    /// <summary>
    /// Gets the return URL supplied for the current authentication flow.
    /// </summary>
    /// <remarks>
    /// The value represents input from the current URL and should not be treated as a trusted navigation target without validation.
    /// </remarks>
    protected string? ReturnUrl { get; private set; }

    /// <summary>
    /// Gets whether focus was requested for the current page.
    /// </summary>
    protected bool ShouldFocus { get; private set; }

    /// <summary>
    /// Gets the identifier supplied for the current authentication flow.
    /// </summary>
    protected string? Identifier { get; private set; }

    /// <summary>
    /// Gets whether authentication flow query parameters should be removed from the browser URL after they have been parsed.
    /// Default is <c>true</c>.
    /// </summary>
    protected virtual bool ClearUAuthQueryAfterParse => true;

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

        ShouldFocus = query.TryGetValue(UAuthConstants.Query.Focus, out var focus) && focus == "1";
        ReturnUrl = query.TryGetValue(UAuthConstants.Query.ReturnUrl, out var ru) ? ru.ToString() : null;
        Identifier = query.TryGetValue(UAuthConstants.Query.Identifier, out var id) ? id.ToString() : null;

        UAuthPayload = null;

        if (query.TryGetValue(UAuthConstants.Query.Payload, out var raw) && !string.IsNullOrWhiteSpace(raw))
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

        _needsClear = ClearUAuthQueryAfterParse && HasUAuthPageQuery(query);
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
            var cleanUri = BuildUriWithoutConsumedUAuthQuery();

            if (!string.Equals(cleanUri, Nav.Uri, StringComparison.Ordinal))
                Nav.NavigateTo(cleanUri, replace: true);
        }
    }

    /// <summary>
    /// Consumes a pending focus request.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when a focus request was pending, otherwise <see langword="false"/>.
    /// </returns>
    protected bool ConsumeFocus()
    {
        if (!ShouldFocus)
            return false;

        ShouldFocus = false;
        return true;
    }

    /// <summary>
    /// Attempts to consume the current authentication flow payload. A payload can be consumed only once for a parsed URL.
    /// </summary>
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

    private string BuildUriWithoutConsumedUAuthQuery()
    {
        var uri = Nav.ToAbsoluteUri(Nav.Uri);
        var query = QueryHelpers.ParseQuery(uri.Query);

        var remaining = query
            .Where(x => !IsConsumedUAuthQueryParameter(x.Key))
            .SelectMany(
                x => x.Value,
                (x, value) => new KeyValuePair<string, string?>(
                    x.Key,
                    value));

        return QueryHelpers.AddQueryString(
            uri.GetLeftPart(UriPartial.Path),
            remaining);
    }

    private static bool IsConsumedUAuthQueryParameter(string key)
    {
        return key is
            UAuthConstants.Query.Payload or
            UAuthConstants.Query.Focus or
            UAuthConstants.Query.ReturnUrl or
            UAuthConstants.Query.Identifier;
    }

    private static bool HasUAuthPageQuery(IDictionary<string, Microsoft.Extensions.Primitives.StringValues> query)
    {
        return query.ContainsKey(UAuthConstants.Query.Payload)
            || query.ContainsKey(UAuthConstants.Query.Focus)
            || query.ContainsKey(UAuthConstants.Query.ReturnUrl)
            || query.ContainsKey(UAuthConstants.Query.Identifier);
    }
}