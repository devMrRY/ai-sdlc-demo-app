using InvoiceApp.Api;
using InvoiceApp.Domain;
using Xunit;

namespace InvoiceApp.Tests;

public class ApprovalServiceTests
{
    private static Invoice InvoiceWith(string id, InvoiceStatus status) =>
        new() { Id = id, CustomerName = "Test School", Amount = 100m, Status = status };

    private static ApprovalService ServiceFor(params Invoice[] invoices) =>
        new(new FakeInvoiceRepository(invoices));

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

    [Fact]
    public void Submit_MovesDraftToPendingApproval()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.Draft);

        var result = ServiceFor(invoice).Submit("INV-1");

        Assert.Equal(InvoiceStatus.PendingApproval, result.Status);
    }

    [Fact]
    public void Submit_RejectsInvoiceThatIsNotADraft()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.Approved));

        Assert.Throws<InvalidOperationException>(() => service.Submit("INV-1"));
    }

    [Fact]
    public void Approve_SetsStatusApproverAndTimestamp()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.PendingApproval));

        var result = service.Approve("INV-1", "demo.manager", "Manager");

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal("demo.manager", result.DecidedBy);
        Assert.NotNull(result.DecidedAt);
    }

    [Fact]
    public void Approve_ManagerAtLimit_Succeeds()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval);
        invoice.Amount = 50_000m;
        var service = ServiceFor(invoice);

        var result = service.Approve("INV-1", "demo.manager", "Manager");

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal("demo.manager", result.DecidedBy);
        Assert.NotNull(result.DecidedAt);
    }

    [Fact]
    public void Approve_ManagerAboveLimit_FailsWithoutMutation()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval);
        invoice.Amount = 50_001m;
        var originalState = SnapshotInvoice(invoice);
        var service = ServiceFor(invoice);

        var exception = Assert.Throws<InvalidOperationException>(
            () => service.Approve("INV-1", "demo.manager", "Manager"));

        Assert.False(string.IsNullOrWhiteSpace(exception.Message));
        AssertInvoiceUnchanged(originalState, invoice);
    }

    [Fact]
    public void Approve_DirectorAtManagerThresholdPlusOne_Succeeds()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval);
        invoice.Amount = 50_001m;
        var service = ServiceFor(invoice);

        var result = service.Approve("INV-1", "demo.director", "Director");

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal("demo.director", result.DecidedBy);
        Assert.NotNull(result.DecidedAt);
    }

    [Fact]
    public void Approve_DirectorBelowManagerThreshold_Succeeds()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval);
        invoice.Amount = 25_000m;
        var service = ServiceFor(invoice);

        var result = service.Approve("INV-1", "demo.director", "Director");

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal("demo.director", result.DecidedBy);
        Assert.NotNull(result.DecidedAt);
    }

    [Fact]
    public void Approve_DirectorAtLimit_Succeeds()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval);
        invoice.Amount = 500_000m;
        var service = ServiceFor(invoice);

        var result = service.Approve("INV-1", "demo.director", "Director");

        Assert.Equal(InvoiceStatus.Approved, result.Status);
        Assert.Equal("demo.director", result.DecidedBy);
        Assert.NotNull(result.DecidedAt);
    }

    [Fact]
    public void Approve_ManagerAtDirectorLimit_FailsWithoutMutation()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval);
        invoice.Amount = 500_000m;
        var originalState = SnapshotInvoice(invoice);
        var service = ServiceFor(invoice);

        var exception = Assert.Throws<InvalidOperationException>(
            () => service.Approve("INV-1", "demo.manager", "Manager"));

        Assert.False(string.IsNullOrWhiteSpace(exception.Message));
        AssertInvoiceUnchanged(originalState, invoice);
    }

    [Fact]
    public void Approve_UnsupportedRole_FailsWithoutMutation()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval);
        var originalState = SnapshotInvoice(invoice);
        var service = ServiceFor(invoice);

        var exception = Assert.Throws<InvalidOperationException>(
            () => service.Approve("INV-1", "demo.approver", "Approver"));

        Assert.False(string.IsNullOrWhiteSpace(exception.Message));
        AssertInvoiceUnchanged(originalState, invoice);
    }

    [Fact]
    public void Approve_UnsupportedRoleForHighValueInvoice_FailsWithoutMutation()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval);
        invoice.Amount = 500_001m;
        var originalState = SnapshotInvoice(invoice);
        var service = ServiceFor(invoice);

        var exception = Assert.Throws<InvalidOperationException>(
            () => service.Approve("INV-1", "demo.approver", "Approver"));

        Assert.False(string.IsNullOrWhiteSpace(exception.Message));
        AssertInvoiceUnchanged(originalState, invoice);
        Assert.Empty(invoice.ApprovalHistory);
    }

    [Fact]
    public void Approve_DirectorAboveLimit_RecordsProvisionalApprovalAndDistinctDirectorFinalizes()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval);
        invoice.Amount = 500_001m;
        var service = ServiceFor(invoice);

        var firstApproval = service.Approve("INV-1", "demo.director", "Director");

        Assert.Equal(InvoiceStatus.PendingApproval, firstApproval.Status);
        Assert.Null(firstApproval.DecidedBy);
        Assert.Null(firstApproval.DecidedAt);
        var provisionalRecord = Assert.Single(firstApproval.ApprovalHistory);
        Assert.Equal("demo.director", provisionalRecord.Approver);
        Assert.Equal("Director", provisionalRecord.Role);
        Assert.NotEqual(default, provisionalRecord.ApprovedAt);
        var provisionalState = SnapshotInvoice(invoice);

        var managerException = Assert.Throws<InvalidOperationException>(
            () => service.Approve("INV-1", "demo.manager", "Manager"));

        Assert.Contains("Director", managerException.Message);
        AssertInvoiceUnchanged(provisionalState, invoice);

        var finalApproval = service.Approve("INV-1", "different.director", "Director");

        Assert.Equal(InvoiceStatus.Approved, finalApproval.Status);
        Assert.Equal("different.director", finalApproval.DecidedBy);
        Assert.NotNull(finalApproval.DecidedAt);
        Assert.Collection(
            finalApproval.ApprovalHistory,
            first => Assert.Equal(("demo.director", "Director"), (first.Approver, first.Role)),
            second => Assert.Equal(("different.director", "Director"), (second.Approver, second.Role)));
    }

    [Fact]
    public void Approve_DistinctDirectorsAboveLimit_SucceedInSequence()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval);
        invoice.Amount = 500_001m;
        var service = ServiceFor(invoice);

        var firstApproval = service.Approve("INV-1", "first.director", "Director");
        Assert.Equal(InvoiceStatus.PendingApproval, firstApproval.Status);
        Assert.Null(firstApproval.DecidedBy);
        Assert.Null(firstApproval.DecidedAt);

        var finalApproval = service.Approve("INV-1", "second.director", "Director");

        Assert.Equal(InvoiceStatus.Approved, finalApproval.Status);
        Assert.Equal("second.director", finalApproval.DecidedBy);
        Assert.NotNull(finalApproval.DecidedAt);
        Assert.Collection(
            finalApproval.ApprovalHistory,
            first => Assert.Equal(("first.director", "Director"), (first.Approver, first.Role)),
            second => Assert.Equal(("second.director", "Director"), (second.Approver, second.Role)));
    }

    [Fact]
    public void Approve_ManagerFirstAboveLimit_IsProvisionalAndDirectorFinalizes()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval);
        invoice.Amount = 500_001m;
        var service = ServiceFor(invoice);

        var firstApproval = service.Approve("INV-1", "demo.manager", "Manager");

        Assert.Equal(InvoiceStatus.PendingApproval, firstApproval.Status);
        Assert.Null(firstApproval.DecidedBy);
        Assert.Null(firstApproval.DecidedAt);
        var provisionalRecord = Assert.Single(firstApproval.ApprovalHistory);
        Assert.Equal("demo.manager", provisionalRecord.Approver);
        Assert.Equal("Manager", provisionalRecord.Role);
        Assert.NotEqual(default, provisionalRecord.ApprovedAt);

        var finalApproval = service.Approve("INV-1", "demo.director", "Director");

        Assert.Equal(InvoiceStatus.Approved, finalApproval.Status);
        Assert.Equal("demo.director", finalApproval.DecidedBy);
        Assert.NotNull(finalApproval.DecidedAt);
        Assert.Equal(2, finalApproval.ApprovalHistory.Count);
    }

    [Fact]
    public void Approve_ManagerSecondApproval_FailsWithoutChangingProvisionalState()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval);
        invoice.Amount = 500_001m;
        var service = ServiceFor(invoice);
        service.Approve("INV-1", "first.manager", "Manager");
        var originalState = SnapshotInvoice(invoice);

        var exception = Assert.Throws<InvalidOperationException>(
            () => service.Approve("INV-1", "second.manager", "Manager"));

        Assert.Contains("Director", exception.Message);
        AssertInvoiceUnchanged(originalState, invoice);
    }

    [Fact]
    public void Approve_DuplicateDirectorSecondApproval_FailsWithoutMutation()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval);
        invoice.Amount = 500_001m;
        var service = ServiceFor(invoice);
        service.Approve("INV-1", "same.director", "Director");
        var originalState = SnapshotInvoice(invoice);

        var exception = Assert.Throws<InvalidOperationException>(
            () => service.Approve("INV-1", "same.director", "Director"));

        Assert.Contains("different user", exception.Message);
        AssertInvoiceUnchanged(originalState, invoice);
    }

    [Theory]
    [InlineData(InvoiceStatus.Approved)]
    [InlineData(InvoiceStatus.Rejected)]
    public void Approve_AlreadyFinalizedInvoice_FailsWithoutMutation(InvoiceStatus status)
    {
        var invoice = InvoiceWith("INV-1", status);
        invoice.Amount = 600_000m;
        invoice.DecidedBy = "final.approver";
        invoice.DecidedAt = DateTime.UtcNow.AddMinutes(-1);
        invoice.DecisionNote = status == InvoiceStatus.Rejected ? "Rejected" : null;
        invoice.ApprovalHistory.Add(
            new ApprovalRecord("first.manager", "Manager", DateTime.UtcNow.AddMinutes(-2)));
        var originalState = SnapshotInvoice(invoice);
        var service = ServiceFor(invoice);

        var exception = Assert.Throws<InvalidOperationException>(
            () => service.Approve("INV-1", "another.director", "Director"));

        Assert.False(string.IsNullOrWhiteSpace(exception.Message));
        AssertInvoiceUnchanged(originalState, invoice);
    }

    [Fact]
    public void Approve_InvalidPendingHistoryOrder_FailsWithoutMutation()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval);
        invoice.Amount = 600_000m;
        invoice.ApprovalHistory.Add(
            new ApprovalRecord("first.manager", "Manager", DateTime.UtcNow.AddMinutes(-2)));
        invoice.ApprovalHistory.Add(
            new ApprovalRecord("second.director", "Director", DateTime.UtcNow.AddMinutes(-1)));
        var originalState = SnapshotInvoice(invoice);
        var service = ServiceFor(invoice);

        var exception = Assert.Throws<InvalidOperationException>(
            () => service.Approve("INV-1", "third.director", "Director"));

        Assert.Contains("approval history", exception.Message);
        AssertInvoiceUnchanged(originalState, invoice);
    }

    [Fact]
    public async Task Approve_ConcurrentFirstApprovals_DoNotCorruptProvisionalHistory()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval);
        invoice.Amount = 600_000m;
        var repository = new InMemoryInvoiceRepository();
        repository.Add(invoice);
        var service = new ApprovalService(repository);
        using var startBarrier = new Barrier(participantCount: 3);

        var approvalAttempts = new[]
        {
            Task.Run(() => TryApprove("first.manager")),
            Task.Run(() => TryApprove("second.manager")),
        };
        Assert.True(startBarrier.SignalAndWait(TimeSpan.FromSeconds(10)));

        var results = await Task.WhenAll(approvalAttempts);
        var successfulAttempt = Assert.Single(results, result => result.Invoice is not null);
        Assert.Null(successfulAttempt.Error);
        var failedAttempt = Assert.Single(results, result => result.Error is not null);
        Assert.Contains("approver", failedAttempt.Error!.Message, StringComparison.OrdinalIgnoreCase);

        var storedInvoice = Assert.IsType<Invoice>(repository.GetById("INV-1"));
        Assert.Equal(InvoiceStatus.PendingApproval, storedInvoice.Status);
        var provisionalRecord = Assert.Single(storedInvoice.ApprovalHistory);
        Assert.Contains(
            provisionalRecord.Approver,
            new[] { "first.manager", "second.manager" });
        Assert.Equal("Manager", provisionalRecord.Role);
        Assert.NotEqual(default, provisionalRecord.ApprovedAt);
        Assert.Null(storedInvoice.DecidedBy);
        Assert.Null(storedInvoice.DecidedAt);
        Assert.Null(storedInvoice.DecisionNote);

        var finalApproval = service.Approve("INV-1", "demo.director", "Director");

        Assert.Equal(InvoiceStatus.Approved, finalApproval.Status);
        Assert.Equal("demo.director", finalApproval.DecidedBy);
        Assert.NotNull(finalApproval.DecidedAt);
        Assert.Equal(2, finalApproval.ApprovalHistory.Count);
        Assert.Equal(provisionalRecord, finalApproval.ApprovalHistory[0]);
        Assert.Equal("demo.director", finalApproval.ApprovalHistory[1].Approver);
        Assert.Equal("Director", finalApproval.ApprovalHistory[1].Role);

        (Invoice? Invoice, InvalidOperationException? Error) TryApprove(string approver)
        {
            Assert.True(startBarrier.SignalAndWait(TimeSpan.FromSeconds(10)));
            try
            {
                return (service.Approve("INV-1", approver, "Manager"), null);
            }
            catch (InvalidOperationException exception)
            {
                return (null, exception);
            }
        }
    }

    [Fact]
    public void Approve_RejectsInvoiceThatIsStillADraft()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.Draft);
        var originalState = SnapshotInvoice(invoice);
        var service = ServiceFor(invoice);

        Assert.Throws<InvalidOperationException>(
            () => service.Approve("INV-1", "demo.manager", "Manager"));

        AssertInvoiceUnchanged(originalState, invoice);
    }

    [Fact]
    public void Approve_ThrowsWhenInvoiceDoesNotExist()
    {
        var service = ServiceFor();

        Assert.Throws<KeyNotFoundException>(
            () => service.Approve("INV-404", "demo.manager", "Manager"));
    }

    [Fact]
    public void Reject_RecordsReasonAndApprover()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval);
        var originalApprovalHistory = invoice.ApprovalHistory.ToList();
        var service = ServiceFor(invoice);

        var result = service.Reject("INV-1", "demo.approver", "Duplicate invoice");

        Assert.Equal(InvoiceStatus.Rejected, result.Status);
        Assert.Equal("demo.approver", result.DecidedBy);
        Assert.NotNull(result.DecidedAt);
        Assert.Equal("Duplicate invoice", result.DecisionNote);
        Assert.Equal(originalApprovalHistory, result.ApprovalHistory);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Reject_RequiresAReasonWithoutMutation(string reason)
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval);
        invoice.DecidedBy = "previous.actor";
        invoice.DecidedAt = DateTime.UtcNow.AddMinutes(-1);
        invoice.DecisionNote = "Existing decision data";
        invoice.ApprovalHistory.Add(
            new ApprovalRecord("first.manager", "Manager", DateTime.UtcNow.AddMinutes(-2)));
        var originalState = SnapshotInvoice(invoice);
        var service = ServiceFor(invoice);

        var exception = Assert.Throws<ArgumentException>(
            () => service.Reject("INV-1", "demo.approver", reason));

        Assert.Equal("A reason is required to reject an invoice.", exception.Message);
        AssertInvoiceUnchanged(originalState, invoice);
    }

    [Theory]
    [InlineData(InvoiceStatus.Approved)]
    [InlineData(InvoiceStatus.Rejected)]
    public void Reject_AlreadyFinalizedInvoice_FailsWithoutMutation(InvoiceStatus status)
    {
        var invoice = InvoiceWith("INV-1", status);
        invoice.DecidedBy = "previous.actor";
        invoice.DecidedAt = DateTime.UtcNow.AddMinutes(-1);
        invoice.DecisionNote = status == InvoiceStatus.Rejected ? "Previous rejection" : null;
        invoice.ApprovalHistory.Add(
            new ApprovalRecord("first.manager", "Manager", DateTime.UtcNow.AddMinutes(-2)));
        var originalState = SnapshotInvoice(invoice);
        var service = ServiceFor(invoice);

        var exception = Assert.Throws<InvalidOperationException>(
            () => service.Reject("INV-1", "demo.approver", "Another reason"));

        Assert.Contains("only pending invoices", exception.Message);
        AssertInvoiceUnchanged(originalState, invoice);
    }
}
