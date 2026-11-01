import { expect, test } from '@playwright/test';
import { InvoiceApprovalPage } from '../pages/invoice-approval-page';

for (const { username, role } of [
  { username: 'demo.approver', role: 'Approver' },
  { username: 'demo.admin', role: 'Admin' },
]) {
  test(`${role} can sign in but cannot approve or reject invoices through the UI`, async ({ page }) => {
    const invoicePage = new InvoiceApprovalPage(page);
    const customerName = `${role} access check ${Date.now()}`;

    await invoicePage.open();
    await invoicePage.signIn(username, 'ChangeMe123!', role);
    const invoiceId = await invoicePage.createAndSubmitInvoice(customerName, 100);

    await page.getByRole('tab', { name: 'Approvals' }).click();
    const pendingInvoice = page.getByRole('listitem').filter({ hasText: invoiceId });

    await expect(pendingInvoice).toContainText(
      'Only Managers and Directors can approve or reject invoices.');
    await expect(
      pendingInvoice.getByRole('button', { name: `Approve invoice ${invoiceId}` })
    ).toHaveCount(0);
    await expect(
      pendingInvoice.getByRole('button', { name: `Reject invoice ${invoiceId}` })
    ).toHaveCount(0);
  });
}
