using MemeTokenHub.ClaimService.Api.Application;
using MemeTokenHub.ClaimService.Api.Domain;
using MongoDB.Driver;

namespace MemeTokenHub.ClaimService.Api.Infrastructure;

public sealed class MongoAttachmentRepository(IMongoDatabase database) : IAttachmentRepository
{
    private readonly IMongoCollection<ClaimAttachment> _attachments = database.GetCollection<ClaimAttachment>("ClaimAttachments");

    public Task CreateAsync(ClaimAttachment attachment, CancellationToken cancellationToken) =>
        _attachments.InsertOneAsync(attachment, cancellationToken: cancellationToken);

    public async Task<IReadOnlyList<ClaimAttachment>> GetByReferencesAsync(
        IReadOnlyCollection<string> objectReferences,
        CancellationToken cancellationToken) =>
        await _attachments.Find(attachment => objectReferences.Contains(attachment.ObjectReference)).ToListAsync(cancellationToken);

    public async Task<ClaimAttachment?> GetByReferenceAsync(string objectReference, CancellationToken cancellationToken) =>
        await _attachments.Find(attachment => attachment.ObjectReference == objectReference).FirstOrDefaultAsync(cancellationToken);

    public async Task<bool> ReplaceAsync(
        ClaimAttachment attachment,
        AttachmentScanStatus expectedStatus,
        CancellationToken cancellationToken)
    {
        FilterDefinition<ClaimAttachment> filter = Builders<ClaimAttachment>.Filter.And(
            Builders<ClaimAttachment>.Filter.Eq(item => item.ObjectReference, attachment.ObjectReference),
            Builders<ClaimAttachment>.Filter.Eq(item => item.ScanStatus, expectedStatus));
        ReplaceOneResult result = await _attachments.ReplaceOneAsync(filter, attachment, cancellationToken: cancellationToken);
        return result.ModifiedCount == 1;
    }
}
