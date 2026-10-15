using System.Data;
using Microsoft.EntityFrameworkCore;
using S379658_HIT339_Assignment2_Library.Database;
using S379658_HIT339_Assignment2_Library.Models;
namespace S379658_HIT339_Assignment2_Library.Services;
public sealed class TransferService
{
    private readonly AppDbContext _db;
    private readonly ReservationService _reservations;
    public TransferService(AppDbContext db, ReservationService reservations)
    { _db = db; _reservations = reservations; }
    public async Task<(bool Ok, string Message)> RequestAsync(int itemId, int destinationId, string notes)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var item = await _db.Items.FindAsync(itemId);
        if (item == null || item.Status is ItemStatus.Borrowed or ItemStatus.Destroyed ||
            await _db.Borrowings.AnyAsync(b => b.ItemId == itemId && b.ReturnDate == null))
            return (false, "Only copies that are not borrowed or destroyed can be transferred.");
        if (item.BranchId == destinationId || !await _db.LibraryBranches.AnyAsync(b => b.Id == destinationId && b.IsActive))
            return (false, "Select a different active destination branch.");
        if (await _db.ItemTransfers.AnyAsync(t => t.ItemId == itemId && t.Status == TransferStatus.Pending))
            return (false, "A transfer is already pending for this copy.");
        if (await _db.Reservations.AnyAsync(r => r.ItemId == itemId &&
            (r.Status == ReservationStatus.Ready || r.Status == ReservationStatus.Pending)))
            return (false, "Complete or cancel current holds before transferring this copy.");
        _db.ItemTransfers.Add(new ItemTransfer { ItemId = itemId, SourceBranchId = item.BranchId,
            DestinationBranchId = destinationId, Notes = (notes ?? "").Trim() });
        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        return (true, "Transfer requested. Complete it once the copy arrives at its new branch.");
    }
    public async Task<(bool Ok, string Message)> CompleteAsync(int transferId)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var transfer = await _db.ItemTransfers.Include(t => t.Item).FirstOrDefaultAsync(t => t.Id == transferId);
        if (transfer == null || transfer.Status != TransferStatus.Pending) return (false, "Pending transfer not found.");
        if (transfer.Item.BranchId != transfer.SourceBranchId || transfer.Item.Status == ItemStatus.Borrowed)
            return (false, "This item has changed status or branch; transfer cannot be completed.");
        transfer.Item.BranchId = transfer.DestinationBranchId;
        transfer.Status = TransferStatus.Completed;
        transfer.CompletedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        return (true, "Copy received and holding branch updated.");
    }
    public async Task<(bool Ok, string Message)> CancelAsync(int transferId)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var transfer = await _db.ItemTransfers.FindAsync(transferId);
        if (transfer == null || transfer.Status != TransferStatus.Pending) return (false, "Pending transfer not found.");
        transfer.Status = TransferStatus.Cancelled;
        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        return (true, "Transfer cancelled.");
    }
}
