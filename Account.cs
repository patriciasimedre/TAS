using System;
using System.Collections.Generic;
using System.Text;

namespace bank
{
    public class Account
    {
        private float balance;          // Soldul curent al contului
        private float minBalance = 10;  // Suma minima care trebuie sa ramana dupa un transfer
        private string currency = "RON"; // default currency
        private ICurrencyConverter? currencyConverter; // Convertor pentru operatii valutare
        
        // ============== FUNCȚIONALITATE NOUĂ: ISTORIC TRANZACȚII ==============
        private List<Transaction> transactionHistory = new List<Transaction>();
        
        // ============== FUNCȚIONALITATE NOUĂ: BLOCARE CONT ==============
        private bool isLocked = false;  // Starea contului (blocat/deblocat)
        private DateTime? lockedUntil = null; // Data pana cand este blocat contul
        
        // ============== FUNCȚIONALITATE NOUĂ: LIMITE ZILNICE ==============
        private float dailyTransferLimit = 5000.00f; // Limita maxima de transfer pe zi
        private float totalTransferredToday = 0.00f; // Total transferat astazi
        private DateTime lastTransferDate = DateTime.MinValue; // Data ultimului transfer
        
        // ============== FUNCȚIONALITATE NOUĂ: DOBÂNDĂ ==============
        private float interestRate = 0.02f; // Rata dobanzii (2% lunar)
        private DateTime lastInterestDate = DateTime.Now; // Data ultimei aplicari a dobanzii

        // Constructor implicit (creeaza un cont gol)
        public Account(string currency = "RON")
        {
            balance = 0;
            this.currency = currency;
        }

        // Constructor cu valoare initiala
        public Account(int value, string currency = "RON")
        {
            balance = value;
            this.currency = currency;
        }

        // Constructor cu valoare initiala si currency converter
        public Account(float value, ICurrencyConverter converter)
        {
            balance = value;
            currencyConverter = converter;
        }

        // Adauga bani in cont
        public void Deposit(float amount)
        {
            // ============== FUNCȚIONALITATE NOUĂ: VERIFICARE CONT BLOCAT ==============
            if (isLocked && lockedUntil.HasValue && DateTime.Now < lockedUntil.Value)
            {
                throw new AccountLockedException($"Contul este blocat pana la {lockedUntil.Value}");
            }
            
            balance += amount;
            
            // ============== FUNCȚIONALITATE NOUĂ: ÎNREGISTRARE ÎN ISTORIC ==============
            AddTransaction("Deposit", amount, balance);
        }

        // Retrage bani din cont
        public void Withdraw(float amount)
        {
            // ============== FUNCȚIONALITATE NOUĂ: VERIFICARE CONT BLOCAT ==============
            if (isLocked && lockedUntil.HasValue && DateTime.Now < lockedUntil.Value)
            {
                throw new AccountLockedException($"Contul este blocat pana la {lockedUntil.Value}");
            }
            
            balance -= amount;
            
            // ============== FUNCȚIONALITATE NOUĂ: ÎNREGISTRARE ÎN ISTORIC ==============
            AddTransaction("Withdraw", -amount, balance);
        }

        // Transfer normal intre doua conturi
        public void TransferFunds(Account destination, float amount)
        {
            destination.Deposit(amount);
            Withdraw(amount);
        }

        // ===============================================================
        // CONVERSII VALUTARE - metode simple
        // ===============================================================

        // Converteste RON in EUR
        public float ConvertRonToEur(float amountRon)
        {
            if (amountRon < 0)
                throw new ArgumentException("Suma nu poate fi negativa");

            float rate = currencyConverter!.GetEurToRonRate();
            return amountRon / rate; // RON / (RON per EUR) = EUR
        }

        // Converteste EUR in RON
        public float ConvertEurToRon(float amountEur)
        {
            if (amountEur < 0)
                throw new ArgumentException("Suma nu poate fi negativa");

            float rate = currencyConverter!.GetEurToRonRate();
            return amountEur * rate; // EUR * (RON per EUR) = RON
        }

        // Transfer RON -> EUR
        public void TransferRonToEur(Account destination, float amountRon)
        {
            if (amountRon > balance)
                throw new NotEnoughFundsException();

            float amountEur = ConvertRonToEur(amountRon);
            Withdraw(amountRon);
            destination.Deposit(amountEur);
        }

        // Transfer EUR -> RON
        public void TransferEurToRon(Account destination, float amountEur)
        {
            if (amountEur > balance)
                throw new NotEnoughFundsException();

            float amountRon = ConvertEurToRon(amountEur);
            Withdraw(amountEur);
            destination.Deposit(amountRon);
        }

        // ===============================================================
        // Functie principala testata: TransferMinFunds()
        // ===============================================================
        // Aceasta metoda realizeaza un transfer intre doua conturi,
        // dar respecta reguli suplimentare de siguranta:
        //  (1) Suma trebuie sa fie pozitiva (> 0)
        //  (2) Suma nu poate depasi soldul curent
        //  (3) Dupa transfer trebuie sa ramana cel putin minBalance (10 lei)
        //  (4) Suma nu poate depasi 40% din soldul initial
        // Daca oricare dintre aceste conditii este incalcata,
        // se arunca exceptia NotEnoughFundsException.
        // ===============================================================

        public Account TransferMinFunds(Account destination, float amount)
        {
            // (1) Verificare suma negativa sau zero
            if (amount <= 0)
                throw new NotEnoughFundsException();

            // (2) Verificare daca suma depaseste soldul curent
            if (amount > Balance)
                throw new NotEnoughFundsException();

            // (3) Verificare sold ramas dupa transfer
            // -----------------------------------------
            // Uneori pot aparea diferente de rotunjire pentru numere float
            // (ex: 10.999999 in loc de 11.00). Pentru a evita erorile,
            // rotunjim valoarea ramasa la doua zecimale.
            float remainingBalance = (float)Math.Round(Balance - amount, 2);

            // Daca soldul ramas este mai mic decat pragul minim admis (10 lei),
            // se considera transfer invalid.
            if (remainingBalance < MinBalance)
                throw new NotEnoughFundsException();

            // (4) Verificare daca suma depaseste 40% din soldul curent
            // (Protectie suplimentara impotriva transferurilor prea mari)
            if (amount > Balance * 0.4f)
                throw new NotEnoughFundsException();

            // ============== FUNCȚIONALITATE NOUĂ: VERIFICARE LIMITĂ ZILNICĂ ==============
            CheckDailyLimit(amount);
            
            // (5) Daca toate verificarile sunt trecute, se efectueaza transferul
            Withdraw(amount);
            destination.Deposit(amount);

            // Optional: mesaj informativ in consola (pentru debugging)
            Console.WriteLine($"Transfer reusit: {amount} lei trimisi. Sold ramas: {remainingBalance} lei.");

            return destination;
        }

    // ===============================================================
    // Transfer valutar: converteste intre valute folosind un convertor
    // ===============================================================
    // Transfera `amount` din acest cont (in valuta sa) catre
    // contul destinatie in `targetCurrency` folosind `ICurrencyConverter`.
    // Regulile de validare sunt aceleasi ca la TransferMinFunds
    // (suma > 0, fonduri suficiente, MinBalance, si limita de 40%).
        public Account TransferCurrency(Account destination, float amount, string targetCurrency, ICurrencyConverter converter)
        {
            // refolosim aceleasi reguli de validare ca in TransferMinFunds
            if (amount <= 0)
                throw new NotEnoughFundsException();

            if (amount > Balance)
                throw new NotEnoughFundsException();

            float remainingBalance = (float)Math.Round(Balance - amount, 2);
            if (remainingBalance < MinBalance)
                throw new NotEnoughFundsException();

            if (amount > Balance * 0.4f)
                throw new NotEnoughFundsException();

            // Obtinem cursul EUR->RON din converter
            float eurToRonRate = converter.GetEurToRonRate();
            float converted;

            // Determinam conversia in functie de valutele implicate
            if (Currency == "EUR" && targetCurrency == "RON")
            {
                // EUR -> RON: inmultim cu cursul
                converted = amount * eurToRonRate;
            }
            else if (Currency == "RON" && targetCurrency == "EUR")
            {
                // RON -> EUR: impartim la curs
                converted = amount / eurToRonRate;
            }
            else
            {
                // Acelasi tip de valuta
                converted = amount;
            }

            Withdraw(amount);
            destination.Deposit(converted);

            Console.WriteLine($"Converted {amount} {Currency} to {converted} {targetCurrency} at rate {eurToRonRate}");

            return destination;
        }

        // ============== FUNCȚIONALITATE NOUĂ: BLOCARE/DEBLOCARE CONT ==============
        public void LockAccount(int hours)
        {
            isLocked = true;
            lockedUntil = DateTime.Now.AddHours(hours);
            Console.WriteLine($"Cont blocat pana la {lockedUntil.Value}");
        }
        
        public void UnlockAccount()
        {
            isLocked = false;
            lockedUntil = null;
            Console.WriteLine("Cont deblocat cu succes");
        }
        
        // ============== FUNCȚIONALITATE NOUĂ: VERIFICARE LIMITĂ ZILNICĂ ==============
        private void CheckDailyLimit(float amount)
        {
            // Resetam limita zilnica daca e o noua zi
            if (lastTransferDate.Date != DateTime.Now.Date)
            {
                totalTransferredToday = 0;
                lastTransferDate = DateTime.Now;
            }
            
            if (totalTransferredToday + amount > dailyTransferLimit)
            {
                throw new DailyLimitExceededException(
                    $"Limita zilnica de transfer ({dailyTransferLimit} {currency}) ar fi depasita. " +
                    $"Ai mai transferat astazi: {totalTransferredToday} {currency}");
            }
            
            totalTransferredToday += amount;
        }
        
        // ============== FUNCȚIONALITATE NOUĂ: APLICARE DOBÂNDĂ ==============
        public void ApplyMonthlyInterest()
        {
            TimeSpan timeSinceLastInterest = DateTime.Now - lastInterestDate;
            
            // Aplicam dobanda doar daca a trecut o luna (30 zile)
            if (timeSinceLastInterest.TotalDays >= 30)
            {
                float interest = balance * interestRate;
                balance += interest;
                lastInterestDate = DateTime.Now;
                
                AddTransaction("Monthly Interest", interest, balance);
                Console.WriteLine($"Dobanda aplicata: {interest} {currency}. Sold nou: {balance} {currency}");
            }
        }
        
        // ============== FUNCȚIONALITATE NOUĂ: SETARE RATĂ DOBÂNDĂ ==============
        public void SetInterestRate(float rate)
        {
            if (rate < 0 || rate > 1)
                throw new ArgumentException("Rata dobanzii trebuie sa fie intre 0 si 1 (0-100%)");
            
            interestRate = rate;
            Console.WriteLine($"Rata dobanzii setata la {rate * 100}%");
        }
        
        // ============== FUNCȚIONALITATE NOUĂ: ISTORIC TRANZACȚII ==============
        private void AddTransaction(string type, float amount, float balanceAfter)
        {
            transactionHistory.Add(new Transaction
            {
                Type = type,
                Amount = amount,
                BalanceAfter = balanceAfter,
                Timestamp = DateTime.Now
            });
        }
        
        public List<Transaction> GetTransactionHistory()
        {
            return new List<Transaction>(transactionHistory);
        }
        
        public void PrintTransactionHistory()
        {
            Console.WriteLine($"\n========== Istoric Tranzactii ({currency}) ==========");
            foreach (var transaction in transactionHistory)
            {
                Console.WriteLine(transaction);
            }
            Console.WriteLine("====================================================\n");
        }
        
        // ============== FUNCȚIONALITATE NOUĂ: STATISTICI CONT ==============
        public AccountStatistics GetStatistics()
        {
            if (transactionHistory.Count == 0)
                return new AccountStatistics();
            
            float totalDeposits = 0;
            float totalWithdrawals = 0;
            int depositCount = 0;
            int withdrawalCount = 0;
            
            foreach (var transaction in transactionHistory)
            {
                if (transaction.Amount > 0)
                {
                    totalDeposits += transaction.Amount;
                    depositCount++;
                }
                else
                {
                    totalWithdrawals += Math.Abs(transaction.Amount);
                    withdrawalCount++;
                }
            }
            
            return new AccountStatistics
            {
                TotalDeposits = totalDeposits,
                TotalWithdrawals = totalWithdrawals,
                DepositCount = depositCount,
                WithdrawalCount = withdrawalCount,
                AverageDeposit = depositCount > 0 ? totalDeposits / depositCount : 0,
                AverageWithdrawal = withdrawalCount > 0 ? totalWithdrawals / withdrawalCount : 0,
                TransactionCount = transactionHistory.Count
            };
        }
        
        // ============== FUNCȚIONALITATE NOUĂ: SETARE LIMITĂ ZILNICĂ ==============
        public void SetDailyLimit(float limit)
        {
            if (limit <= 0)
                throw new ArgumentException("Limita zilnica trebuie sa fie pozitiva");
            
            dailyTransferLimit = limit;
            Console.WriteLine($"Limita zilnica de transfer setata la {limit} {currency}");
        }
        
        // ============== FUNCȚIONALITATE NOUĂ: VERIFICARE SECURITATE CONT ==============
        public bool IsAccountSecure()
        {
            // Verifica daca contul respecta regulile de securitate
            bool hasMinBalance = balance >= minBalance;
            bool notLocked = !isLocked || (lockedUntil.HasValue && DateTime.Now >= lockedUntil.Value);
            bool reasonableBalance = balance >= 0; // Nu permite sold negativ
            
            return hasMinBalance && notLocked && reasonableBalance;
        }

        // Proprietati pentru accesarea valorilor curente
        public float Balance => balance;
        public float MinBalance => minBalance;
        public string Currency => currency;
        public bool IsLocked => isLocked && lockedUntil.HasValue && DateTime.Now < lockedUntil.Value;
        public float DailyTransferLimit => dailyTransferLimit;
        public float TotalTransferredToday => totalTransferredToday;
        public float InterestRate => interestRate;
    }

    // ===============================================================
    // Exceptie personalizata pentru situatii de fonduri insuficiente
    // ===============================================================
    public class NotEnoughFundsException : ApplicationException
    {
        public NotEnoughFundsException()
            : base("Error in the application.") // mesaj simplu, compatibil cu testele existente
        {
        }
    }
    
    // ============== FUNCȚIONALITATE NOUĂ: EXCEPȚII PERSONALIZATE ==============
    public class AccountLockedException : ApplicationException
    {
        public AccountLockedException(string message) : base(message) { }
    }
    
    public class DailyLimitExceededException : ApplicationException
    {
        public DailyLimitExceededException(string message) : base(message) { }
    }
    
    // ============== FUNCȚIONALITATE NOUĂ: CLASĂ PENTRU TRANZACȚII ==============
    public class Transaction
    {
        public string Type { get; set; } = string.Empty;
        public float Amount { get; set; }
        public float BalanceAfter { get; set; }
        public DateTime Timestamp { get; set; }
        
        public override string ToString()
        {
            return $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] {Type}: {Amount:F2} | Sold dupa: {BalanceAfter:F2}";
        }
    }
    
    // ============== FUNCȚIONALITATE NOUĂ: CLASĂ PENTRU STATISTICI ==============
    public class AccountStatistics
    {
        public float TotalDeposits { get; set; }
        public float TotalWithdrawals { get; set; }
        public int DepositCount { get; set; }
        public int WithdrawalCount { get; set; }
        public float AverageDeposit { get; set; }
        public float AverageWithdrawal { get; set; }
        public int TransactionCount { get; set; }
        
        public override string ToString()
        {
            return $@"Statistici Cont:
  Total Depuneri: {TotalDeposits:F2} ({DepositCount} tranzactii, medie: {AverageDeposit:F2})
  Total Retrageri: {TotalWithdrawals:F2} ({WithdrawalCount} tranzactii, medie: {AverageWithdrawal:F2})
  Total Tranzactii: {TransactionCount}";
        }
    }
}