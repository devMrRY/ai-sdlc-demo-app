namespace InvoiceApp.Domain;

public interface IInvoiceRepository
{
    IReadOnlyList<Invoice> GetAll();
    Invoice? GetById(string id);
    IReadOnlyList<Invoice> GetByIds(IEnumerable<string> ids);
    Invoice Add(Invoice invoice);
    void Update(Invoice invoice);
    T ExecuteAtomically<T>(string id, Func<Invoice?, T> operation);
    bool Remove(string id);
}
