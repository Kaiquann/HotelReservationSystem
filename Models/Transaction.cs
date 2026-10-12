using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace HotelReservationSystem.Models;

public enum TransactionStatus
{
    [Display(Name = "Pending")] Pending, // Initial transaction, waiting for payment

    [Display(Name = "Confirmed")] Confirmed, // Payment received, transaction confirmed

    [Display(Name = "Canceled")] Canceled, // Transaction was canceled

    [Display(Name = "Failed")] Failed, // Transaction failed (payment failed, etc.)

    [Display(Name = "Pending Refund")] PendingRefund, // Transaction is pending refund

    [Display(Name = "Refunded")] Refunded // Transaction was refunded
}

public class Transaction
{
    [Key] public int Id { get; set; }

    [MaxLength(255)] public string TransactionNumber { get; set; } = string.Empty;

    [MaxLength(255)] public string SessionId { get; set; } = string.Empty;

    [MaxLength(255)] public string PaymentIntentId { get; set; } = string.Empty;

    [MaxLength(255)] public string PaymentMethod { get; set; } = string.Empty;

    [Precision(10, 2)] public decimal ServiceCharge { get; set; } = 0;

    [Precision(10, 2)] public decimal TotalAmount { get; set; } = 0;

    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // FK
    public int UserId { get; set; }

    // Navigation property
    public User User { get; set; } = null!;
}