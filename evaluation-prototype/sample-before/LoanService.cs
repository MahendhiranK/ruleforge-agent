namespace LoanProcessing.Api;

public class LoanService
{
    public decimal CalculateLoan(decimal amount)
    {
        int unused = 100;

        // if (amount > 0) return amount;

        if (amount > 0)
        {
            if (amount > 1000)
            {
                return amount * 1.05m;
            }
        }

        return amount;
    }
}
