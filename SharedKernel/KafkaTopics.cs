namespace SharedKernel;

public static class KafkaTopics
{
    // User events
    public const string UserRegistered = "user.registered";
    public const string UserUpdated = "user.updated";
    public const string CustomerCreated = "customer.created";

    // Product events
    public const string ProductCreated = "product.created";
    public const string ProductUpdated = "product.updated";
    public const string ProductDeleted = "product.deleted";

    // Cart events
    public const string CartUpdated = "cart.updated";
    public const string CartCleared = "cart.cleared";

    // Order events
    public const string OrderCreated = "order.created";
    public const string OrderUpdated = "order.updated";
    public const string OrderCancelled = "order.cancelled";

    // Invoice events
    public const string InvoiceCreated = "invoice.created";
    public const string InvoiceExported = "invoice.exported";
}
