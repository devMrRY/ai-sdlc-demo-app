namespace InvoiceApp.Domain;

/// <summary>
/// Moves invoices through submission, approval, and rejection.
/// </summary>
public class ApprovalService
{
    private readonly IInvoiceRepository _repository;

    public ApprovalService(IInvoiceRepository repository)
    {
        _repository = repository;
    }

    public Invoice Submit(string invoiceId)
    {
        return _repository.ExecuteAtomically(invoiceId, currentInvoice =>
        {
            var invoice = Find(currentInvoice, invoiceId);

            if (invoice.Status != InvoiceStatus.Draft)
            {
                throw new InvalidOperationException(
                    $"Invoice {invoiceId} is {invoice.Status}; only drafts can be submitted.");
            }

            invoice.Status = InvoiceStatus.PendingApproval;
            _repository.Update(invoice);
            return invoice;
        });
    }

    public Invoice Approve(string invoiceId, string approver, string role)
    {
        return _repository.ExecuteAtomically(invoiceId, currentInvoice =>
        {
            var invoice = Find(currentInvoice, invoiceId);
            EnsurePending(invoice);

            if (invoice.Amount > 500_000m)
            {
                return ApproveHighValueInvoice(invoice, approver, role);
            }

            EnsureApprovalAllowed(invoice, role);
            EnsureNoExistingApprovalHistory(invoice);

            var approvedAt = DateTime.UtcNow;
            invoice.ApprovalHistory.Add(new ApprovalRecord(approver, role, approvedAt));
            FinalizeApproval(invoice, approver, approvedAt);
            _repository.Update(invoice);
            return invoice;
        });
    }

    public Invoice Reject(string invoiceId, string approver, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("A reason is required to reject an invoice.");
        }

        return _repository.ExecuteAtomically(invoiceId, currentInvoice =>
        {
            var invoice = Find(currentInvoice, invoiceId);
            EnsurePending(invoice);

            invoice.Status = InvoiceStatus.Rejected;
            invoice.DecidedBy = approver;
            invoice.DecidedAt = DateTime.UtcNow;
            invoice.DecisionNote = reason;
            _repository.Update(invoice);
            return invoice;
        });
    }

    private static Invoice Find(Invoice? invoice, string invoiceId) =>
        invoice
        ?? throw new KeyNotFoundException($"Invoice {invoiceId} was not found.");

    private static void EnsurePending(Invoice invoice)
    {
        if (invoice.Status != InvoiceStatus.PendingApproval)
        {
            throw new InvalidOperationException(
                $"Invoice {invoice.Id} is {invoice.Status}; only pending invoices can be decided.");
        }
    }

    private static void EnsureApprovalAllowed(Invoice invoice, string role)
    {
        if (role == "Manager" && invoice.Amount <= 50_000m)
        {
            return;
        }

        if (role == "Director" && invoice.Amount <= 500_000m)
        {
            return;
        }

        var reason = role switch
        {
            "Manager" => "Managers may approve invoices only up to 50000.",
            "Director" => "Directors may approve invoices only up to 500000 in a single approval.",
            _ => $"Role '{role}' is not authorized to approve invoices.",
        };

        throw new InvalidOperationException(reason);
    }

    private Invoice ApproveHighValueInvoice(Invoice invoice, string approver, string role)
    {
        if (invoice.ApprovalHistory.Count == 0)
        {
            if (role is not ("Manager" or "Director"))
            {
                throw new InvalidOperationException(
                    $"Role '{role}' is not authorized to provide a provisional approval.");
            }

            invoice.ApprovalHistory.Add(
                new ApprovalRecord(approver, role, DateTime.UtcNow));
            _repository.Update(invoice);
            return invoice;
        }

        if (invoice.ApprovalHistory.Count != 1)
        {
            throw new InvalidOperationException(
                "The pending invoice has an invalid approval history.");
        }

        var firstApproval = invoice.ApprovalHistory[0];
        if (string.Equals(firstApproval.Approver, approver, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The second approval must be made by a different user.");
        }

        if (role != "Director")
        {
            throw new InvalidOperationException(
                "The second approver for invoices above 500000 must be a Director.");
        }

        var approvedAt = DateTime.UtcNow;
        invoice.ApprovalHistory.Add(new ApprovalRecord(approver, role, approvedAt));
        FinalizeApproval(invoice, approver, approvedAt);
        _repository.Update(invoice);
        return invoice;
    }

    private static void EnsureNoExistingApprovalHistory(Invoice invoice)
    {
        if (invoice.ApprovalHistory.Count != 0)
        {
            throw new InvalidOperationException(
                "A single-approval invoice cannot already have approval history.");
        }
    }

    private static void FinalizeApproval(Invoice invoice, string approver, DateTime approvedAt)
    {
        invoice.Status = InvoiceStatus.Approved;
        invoice.DecidedBy = approver;
        invoice.DecidedAt = approvedAt;
    }
}
