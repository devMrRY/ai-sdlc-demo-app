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
    public void Approve_DirectorAboveLimit_FailsWithoutMutation()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval);
        invoice.Amount = 500_001m;
        var originalState = SnapshotInvoice(invoice);
        var service = ServiceFor(invoice);

        var exception = Assert.Throws<InvalidOperationException>(
            () => service.Approve("INV-1", "demo.director", "Director"));

        Assert.False(string.IsNullOrWhiteSpace(exception.Message));
        AssertInvoiceUnchanged(originalState, invoice);
    }

    [Fact]
    public void Approve_RejectsInvoiceThatIsStillADraft()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.Draft));

        Assert.Throws<InvalidOperationException>(
            () => service.Approve("INV-1", "demo.manager", "Manager"));
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
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.PendingApproval));

        var result = service.Reject("INV-1", "demo.approver", "Duplicate invoice");

        Assert.Equal(InvoiceStatus.Rejected, result.Status);
        Assert.Equal("Duplicate invoice", result.DecisionNote);
    }

    [Fact]
    public void Reject_RequiresAReason()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.PendingApproval));

        Assert.Throws<ArgumentException>(() => service.Reject("INV-1", "demo.approver", "  "));
    }
}
