namespace CarInsurance.Api.Models
{
    public class Claim
    { 
        public Claim(long policyId, DateOnly claimDate, string description, decimal amount)
        {
            PolicyId = policyId;
            ClaimDate = claimDate;
            Description = description;
            Amount = amount;
        }

        public long Id { get; set; }
        public long PolicyId { get; set; }
        public DateOnly ClaimDate { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
    }
}
