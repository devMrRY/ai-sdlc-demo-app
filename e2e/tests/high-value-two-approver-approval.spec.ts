import { expect, test } from '@playwright/test';
import { InvoiceApprovalPage } from '../pages/invoice-approval-page';

test('high-value invoice stays pending until a distinct Director approves', async ({ page }) => {
  const invoicePage = new InvoiceApprovalPage(page);
  const customerName = `Two approver check ${Date.now()}`;

  await invoicePage.open();
  await invoicePage.signIn('demo.manager', 'ChangeMe123!', 'Manager');
  const invoiceId = await invoicePage.createAndSubmitInvoice(customerName, 500_001);

  await invoicePage.approve(invoiceId);
  await invoicePage.expectProvisionalApproval(invoiceId, 'demo.manager', 'Manager');
  const provisionalInvoice = await invoicePage.getAuthenticatedInvoice(invoiceId);
  expect(provisionalInvoice.status).toBe('PendingApproval');
  expect(provisionalInvoice.decidedBy).toBeNull();
  expect(provisionalInvoice.decidedAt).toBeNull();
  expect(provisionalInvoice.approvalHistory).toHaveLength(1);
  expect(provisionalInvoice.approvalHistory[0]).toMatchObject({
    approver: 'demo.manager',
    role: 'Manager',
  });
  expect(provisionalInvoice.approvalHistory[0].approvedAt).toBeTruthy();

  await invoicePage.signOut();
  await invoicePage.signIn('demo.director', 'ChangeMe123!', 'Director');
  await invoicePage.approve(invoiceId);
  await invoicePage.expectApproved(invoiceId, customerName);

  const approvedInvoice = await invoicePage.getAuthenticatedInvoice(invoiceId);
  expect(approvedInvoice.status).toBe('Approved');
  expect(approvedInvoice.decidedBy).toBe('demo.director');
  expect(approvedInvoice.decidedAt).toBeTruthy();
  expect(approvedInvoice.approvalHistory).toHaveLength(2);
  expect(approvedInvoice.approvalHistory.map((record: { approver: string }) => record.approver))
    .toEqual(['demo.manager', 'demo.director']);
});
