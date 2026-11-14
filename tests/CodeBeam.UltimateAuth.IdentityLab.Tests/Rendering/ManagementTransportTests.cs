using Bunit;
using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.IdentityLab.Components.Pages;
using CodeBeam.UltimateAuth.IdentityLab.Contracts;
using CodeBeam.UltimateAuth.IdentityLab.Services;
using Moq;
using System.Text.Json;

namespace CodeBeam.UltimateAuth.IdentityLab.Tests.Rendering;

public sealed class ManagementTransportTests
{
    [Fact]
    public async Task Scan_uses_browser_transport_and_renders_returned_snapshot()
    {
        await using var context = new PageRenderContext();
        context.AddAdminServices();
        context.Requests.Setup(x => x.SendJsonAsync("/api/lab/admin/repair/scan", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UAuthTransportResult
            {
                Ok = true, Status = 200,
                Body = JsonSerializer.SerializeToElement(new RepairSnapshot([], [], true))
            });
        var panel = context.RenderPage<AdminRepairPanel>();
        await panel.Find("button").ClickAsync();
        panel.WaitForAssertion(() => Assert.Contains("Unlinked Users (0)", panel.Markup));
        context.Requests.Verify(x => x.SendJsonAsync("/api/lab/admin/repair/scan", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Client_rejects_failed_transport_without_exposing_response_details()
    {
        var requests = new Mock<CodeBeam.UltimateAuth.Client.Infrastructure.IUAuthRequestClient>();
        requests.Setup(x => x.SendJsonAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UAuthTransportResult { Status = 500, Body = JsonSerializer.SerializeToElement("private diagnostic") });
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new LabManagementClient(requests.Object).SendAsync<RepairSnapshot>("/api/lab/admin/repair/scan", ct: Xunit.TestContext.Current.CancellationToken));
        Assert.DoesNotContain("private", error.Message);
    }
}
