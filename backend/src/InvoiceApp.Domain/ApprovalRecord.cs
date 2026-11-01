namespace InvoiceApp.Domain;

public sealed record ApprovalRecord(string Approver, string Role, DateTime ApprovedAt);
