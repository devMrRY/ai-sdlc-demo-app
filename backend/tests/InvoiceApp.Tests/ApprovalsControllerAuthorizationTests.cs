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

        await AssertApprovalForbiddenAndUnchanged(response, host);
    }

    [Fact]
    public async Task Approve_ApproverRole_ForbiddenWithoutMutation()
    {
        using var host = new ApiTestHost("Approver", "approver.user");

        var response = await host.Client.PostAsync(
            $"/api/approvals/{host.Invoice.Id}/approve",
            content: null);

        await AssertApprovalForbiddenAndUnchanged(response, host);
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

        await AssertApprovalForbiddenAndUnchanged(response, host);
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

    [Fact]
    public async Task Approve_HighValueFlow_UsesAuthenticatedSessionForBothApprovals()
    {
        using var host = new ApiTestHost("Manager", "manager.user", amount: 600_001m);

        var firstResponse = await host.Client.PostAsJsonAsync(
            $"/api/approvals/{host.Invoice.Id}/approve",
            new { username = "forged.director", role = "Director" });

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        var provisionalInvoice = await firstResponse.Content.ReadFromJsonAsync<Invoice>();
        Assert.NotNull(provisionalInvoice);
        Assert.Equal(InvoiceStatus.PendingApproval, provisionalInvoice.Status);
        Assert.Null(provisionalInvoice.DecidedBy);
        Assert.Null(provisionalInvoice.DecidedAt);
        var firstApproval = Assert.Single(provisionalInvoice.ApprovalHistory);
        Assert.Equal("manager.user", firstApproval.Approver);
        Assert.Equal("Manager", firstApproval.Role);

        var stateAfterFirstApproval = SnapshotInvoice(host.GetInvoice());
        host.SetSession("Manager", "second.manager");
        var invalidSecondResponse = await host.Client.PostAsJsonAsync(
            $"/api/approvals/{host.Invoice.Id}/approve",
            new { username = "forged.director", role = "Director" });

        Assert.Equal(HttpStatusCode.Conflict, invalidSecondResponse.StatusCode);
        Assert.Contains(
            "Director",
            (await invalidSecondResponse.Content.ReadFromJsonAsync<JsonElement>())
                .GetProperty("error")
                .GetString());
        AssertInvoiceUnchanged(stateAfterFirstApproval, host.GetInvoice());

        host.SetSession("Director", "director.user");
        var secondResponse = await host.Client.PostAsJsonAsync(
            $"/api/approvals/{host.Invoice.Id}/approve",
            new { username = "forged.manager", role = "Manager" });

        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        var approvedInvoice = await secondResponse.Content.ReadFromJsonAsync<Invoice>();
        Assert.NotNull(approvedInvoice);
        Assert.Equal(InvoiceStatus.Approved, approvedInvoice.Status);
        Assert.Equal("director.user", approvedInvoice.DecidedBy);
        Assert.NotNull(approvedInvoice.DecidedAt);
        Assert.Collection(
            approvedInvoice.ApprovalHistory,
            first => Assert.Equal(("manager.user", "Manager"), (first.Approver, first.Role)),
            second => Assert.Equal(("director.user", "Director"), (second.Approver, second.Role)));
    }

    private static void AssertForbiddenAndUnchanged(
        HttpResponseMessage response,
        ApiTestHost host)
    {
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        AssertInvoiceUnchanged(host);
        response.Dispose();
    }

    private static async Task AssertApprovalForbiddenAndUnchanged(
        HttpResponseMessage response,
        ApiTestHost host)
    {
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(
            "A Manager or Director role is required to approve the invoice.",
            body.GetProperty("error").GetString());
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

    private static Invoice SnapshotInvoice(Invoice invoice) =>
        new()
        {
            Id = invoice.Id,
            CustomerName = invoice.CustomerName,
            Amount = invoice.Amount,
            Status = invoice.Status,
            CreatedAt = invoice.CreatedAt,
            DecidedBy = invoice.DecidedBy,
            DecidedAt = invoice.DecidedAt,
            DecisionNote = invoice.DecisionNote,
            ApprovalHistory = invoice.ApprovalHistory.ToList(),
        };

    private static void AssertInvoiceUnchanged(ApiTestHost host) =>
        AssertInvoiceUnchanged(host.InitialInvoiceSnapshot, host.GetInvoice());

    private static void AssertInvoiceUnchanged(Invoice expected, Invoice actual)
    {
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.CustomerName, actual.CustomerName);
        Assert.Equal(expected.Amount, actual.Amount);
        Assert.Equal(expected.Status, actual.Status);
        Assert.Equal(expected.CreatedAt, actual.CreatedAt);
        Assert.Equal(expected.DecidedBy, actual.DecidedBy);
        Assert.Equal(expected.DecidedAt, actual.DecidedAt);
        Assert.Equal(expected.DecisionNote, actual.DecisionNote);
        Assert.Equal(expected.ApprovalHistory, actual.ApprovalHistory);
    }

    private sealed class ApiTestHost : IDisposable
    {
        private readonly TestServer _server;
        private readonly SessionStore _sessions;

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
            InitialInvoiceSnapshot = SnapshotInvoice(Invoice);

            _sessions = new SessionStore();
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services
                        .AddControllers()
                        .AddApplicationPart(typeof(ApprovalsController).Assembly);
                    services.AddSingleton(_sessions);
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
                SetSession(role, username);
            }
        }

        public HttpClient Client { get; }
        public InMemoryInvoiceRepository Invoices { get; }
        public Invoice Invoice { get; }
        public Invoice InitialInvoiceSnapshot { get; }

        public Invoice GetInvoice() =>
            Invoices.GetById(Invoice.Id)
            ?? throw new InvalidOperationException($"Test invoice {Invoice.Id} disappeared.");

        public void SetSession(string role, string username)
        {
            var token = _sessions.Create(new UserSession(username, role));
            Client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        public void Dispose()
        {
            Client.Dispose();
            _server.Dispose();
        }
    }
}
