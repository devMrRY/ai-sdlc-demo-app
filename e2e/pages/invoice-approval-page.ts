import { expect, Page } from '@playwright/test';

export class InvoiceApprovalPage {
  constructor(private readonly page: Page) {}

  async open() {
    await this.page.goto('/');
  }

  async signIn(username: string, password: string) {
    await this.page.getByLabel('Username').fill(username);
    await this.page.getByLabel('Password').fill(password);
    await this.page.getByRole('button', { name: 'Sign in' }).click();
    await expect(this.page.getByText(`${username} (Manager)`)).toBeVisible();
  }

  async createAndSubmitInvoice(customerName: string, amount: number) {
    await this.page.getByLabel('Customer name').fill(customerName);
    await this.page.getByLabel('Amount').fill(String(amount));
    await this.page.getByRole('button', { name: 'Create invoice' }).click();

    const invoiceRow = this.page
      .getByRole('row')
      .filter({ hasText: customerName });
    await expect(invoiceRow).toBeVisible();
    await invoiceRow
      .getByRole('button', { name: /Submit invoice .* for approval/ })
      .click();
    await expect(invoiceRow.getByRole('cell').nth(3)).toHaveText('Pending Approval');
    return invoiceRow.getByRole('cell').nth(0).innerText();
  }

  async approveAsManager(invoiceId: string) {
    await this.page.getByRole('tab', { name: 'Approvals' }).click();
    await this.page
      .getByRole('button', { name: `Approve invoice ${invoiceId}` })
      .click();
  }

  async expectPendingApproval(invoiceId: string, customerName: string) {
    await this.page.getByRole('tab', { name: 'Invoices' }).click();
    const invoiceRow = this.page
      .getByRole('row')
      .filter({ hasText: customerName });
    await expect(invoiceRow.getByRole('cell').nth(3)).toHaveText('Pending Approval');

    await this.page.getByRole('tab', { name: 'Approvals' }).click();
    const pendingInvoice = this.page
      .getByRole('listitem')
      .filter({ hasText: invoiceId });
    await expect(pendingInvoice).toBeVisible();
  }

  async getAuthenticatedInvoice(invoiceId: string) {
    const token = await this.page.evaluate(() => {
      const session = JSON.parse(sessionStorage.getItem('session') ?? 'null');
      return session?.token;
    });

    const response = await this.page.request.get(`/api/invoices/${invoiceId}`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    expect(response.ok()).toBeTruthy();
    return response.json();
  }

  async expectDecisionError(message: string) {
    await expect(this.page.getByRole('alert').filter({ hasText: message })).toBeVisible();
  }
}
