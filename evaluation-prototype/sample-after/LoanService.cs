namespace LoanProcessing.Api;

public class LoanService
{
    public decimal CalculateLoan(decimal amount)
    {
        if (amount > 1000)
        {
            return amount * 1.05m;
        }

        return amount;
    }
}
