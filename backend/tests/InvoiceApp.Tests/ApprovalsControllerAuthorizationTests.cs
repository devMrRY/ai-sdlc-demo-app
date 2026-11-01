using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using InvoiceApp.Api;
using InvoiceApp.Api.Auth;
using InvoiceApp.Api.Controllers;
using InvoiceApp.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InvoiceApp.Tests;

public class ApprovalsControllerAuthorizationTests
{
    [Theory]
    [InlineData("Manager", "manager.user")]
    [InlineData("Director", "director.user")]
    public async Task Approve_AllowedRoles_ApprovesInvoice(string role, string username)
    {
        using var host = new ApiTestHost(role, username);

        var response = await host.Client.PostAsync(
            $"/api/approvals/{host.Invoice.Id}/approve",
            content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(InvoiceStatus.Approved, host.GetInvoice().Status);
        Assert.Equal(username, host.GetInvoice().DecidedBy);
        Assert.NotNull(host.GetInvoice().DecidedAt);
    }

    [Theory]
    [InlineData("Manager", "manager.user")]
    [InlineData("Director", "director.user")]
    public async Task Reject_ApprovalRoles_ForbiddenWithoutMutation(string role, string username)
    {
        using var host = new ApiTestHost(role, username);

        var response = await host.Client.PostAsJsonAsync(
            $"/api/approvals/{host.Invoice.Id}/reject",
            new ApprovalsController.RejectRequest("Not permitted"));

        AssertForbiddenAndUnchanged(response, host);
    }

    [Fact]
    public async Task Approve_UnsupportedRole_ForbiddenWithoutMutation()
    {
        using var host = new ApiTestHost("Auditor", "auditor.user");

        var response = await host.Client.PostAsync(
            $"/api/approvals/{host.Invoice.Id}/approve",
            content: null);

        AssertForbiddenAndUnchanged(response, host);
    }

    [Fact]
    public async Task Approve_ApproverRole_ForbiddenWithoutMutation()
    {
        using var host = new ApiTestHost("Approver", "approver.user");

        var response = await host.Client.PostAsync(
            $"/api/approvals/{host.Invoice.Id}/approve",
            content: null);

        AssertForbiddenAndUnchanged(response, host);
    }

    [Theory]
    [InlineData("Approver", "approver.user")]
    [InlineData("Admin", "admin.user")]
    public async Task Reject_RejectionRoles_RejectsInvoice(string role, string username)
    {
        using var host = new ApiTestHost(role, username);

        var response = await host.Client.PostAsJsonAsync(
            $"/api/approvals/{host.Invoice.Id}/reject",
            new ApprovalsController.RejectRequest("Duplicate invoice"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(InvoiceStatus.Rejected, host.GetInvoice().Status);
        Assert.Equal(username, host.GetInvoice().DecidedBy);
        Assert.Equal("Duplicate invoice", host.GetInvoice().DecisionNote);
        Assert.NotNull(host.GetInvoice().DecidedAt);
    }

    [Theory]
    [InlineData("Approver", "approver.user")]
    [InlineData("Admin", "admin.user")]
    public async Task Approve_RejectionRoles_ForbiddenWithoutMutation(string role, string username)
    {
        using var host = new ApiTestHost(role, username);

        var response = await host.Client.PostAsync(
            $"/api/approvals/{host.Invoice.Id}/approve",
            content: null);

        AssertForbiddenAndUnchanged(response, host);
    }

    [Fact]
    public async Task Approve_MissingBearerToken_UnauthorizedWithoutMutation()
    {
        using var host = new ApiTestHost();

        var response = await host.Client.PostAsync(
            $"/api/approvals/{host.Invoice.Id}/approve",
            content: null);

        AssertUnauthorizedAndUnchanged(response, host);
    }

    [Fact]
    public async Task Approve_InvalidBearerToken_UnauthorizedWithoutMutation()
    {
        using var host = new ApiTestHost();
        host.Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "invalid-token");

        var response = await host.Client.PostAsync(
            $"/api/approvals/{host.Invoice.Id}/approve",
            content: null);

        AssertUnauthorizedAndUnchanged(response, host);
    }

    [Fact]
    public async Task Approve_ManagerOverLimit_ReturnsConflictAndClearReasonWithoutMutation()
    {
        using var host = new ApiTestHost("Manager", "manager.user", amount: 50_001m);

        var response = await host.Client.PostAsync(
            $"/api/approvals/{host.Invoice.Id}/approve",
            content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(
            "Managers may approve invoices only up to 50000.",
            body.GetProperty("error").GetString());
        AssertInvoiceUnchanged(host);
    }

    private static void AssertForbiddenAndUnchanged(
        HttpResponseMessage response,
        ApiTestHost host)
    {
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        AssertInvoiceUnchanged(host);
        response.Dispose();
    }

    private static void AssertUnauthorizedAndUnchanged(
        HttpResponseMessage response,
        ApiTestHost host)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        AssertInvoiceUnchanged(host);
        response.Dispose();
    }

    private static void AssertInvoiceUnchanged(ApiTestHost host)
    {
        var invoice = host.GetInvoice();
        Assert.Equal(InvoiceStatus.PendingApproval, invoice.Status);
        Assert.Null(invoice.DecidedBy);
        Assert.Null(invoice.DecidedAt);
        Assert.Null(invoice.DecisionNote);
    }

    private sealed class ApiTestHost : IDisposable
    {
        private readonly TestServer _server;

        public ApiTestHost(string? role = null, string? username = null, decimal amount = 100m)
        {
            Invoices = new InMemoryInvoiceRepository();
            Invoice = Invoices.Add(new Invoice
            {
                Id = "INV-AUTH-1",
                CustomerName = "Authorization test invoice",
                Amount = amount,
                Status = InvoiceStatus.PendingApproval,
            });

            var sessions = new SessionStore();
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services
                        .AddControllers()
                        .AddApplicationPart(typeof(ApprovalsController).Assembly);
                    services.AddSingleton(sessions);
                    services.AddSingleton<IInvoiceRepository>(Invoices);
                    services.AddScoped<ApprovalService>();
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseMiddleware<SessionAuthMiddleware>();
                    app.UseEndpoints(endpoints => endpoints.MapControllers());
                });

            _server = new TestServer(builder);
            Client = _server.CreateClient();

            if (role is not null && username is not null)
            {
                var token = sessions.Create(new UserSession(username, role));
                Client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }
        }

        public HttpClient Client { get; }
        public InMemoryInvoiceRepository Invoices { get; }
        public Invoice Invoice { get; }

        public Invoice GetInvoice() =>
            Invoices.GetById(Invoice.Id)
            ?? throw new InvalidOperationException($"Test invoice {Invoice.Id} disappeared.");

        public void Dispose()
        {
            Client.Dispose();
            _server.Dispose();
        }
    }
}
