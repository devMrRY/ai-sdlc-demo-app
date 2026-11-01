import { expect, test } from '@playwright/test';
import { InvoiceApprovalPage } from '../pages/invoice-approval-page';

test('Manager cannot approve an invoice above 50000 through the UI', async ({ page }) => {
  const invoicePage = new InvoiceApprovalPage(page);
  const customerName = `Manager limit check ${Date.now()}`;

  await invoicePage.open();
  await invoicePage.signIn('demo.manager', 'ChangeMe123!');
  const invoiceId = await invoicePage.createAndSubmitInvoice(customerName, 50_001);

  await invoicePage.approveAsManager(invoiceId);

  await invoicePage.expectDecisionError('Managers may approve invoices only up to 50000.');
  await invoicePage.expectPendingApproval(invoiceId, customerName);

  const invoice = await invoicePage.getAuthenticatedInvoice(invoiceId);
  expect(invoice.status).toBe('PendingApproval');
  expect(invoice.decidedBy).toBeNull();
  expect(invoice.decidedAt).toBeNull();
});
