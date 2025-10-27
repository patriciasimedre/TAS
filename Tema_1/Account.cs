using System;
using System.Collections.Generic;
using System.Text;

namespace bank
{
    public class Account
    {
        private float balance;          // Soldul curent al contului
        private float minBalance = 10;  // Suma minima care trebuie sa ramana dupa un transfer

        // Constructor implicit (creeaza un cont gol)
        public Account()
        {
            balance = 0;
        }

        // Constructor cu valoare initiala
        public Account(int value)
        {
            balance = value;
        }

        // Adauga bani in cont
        public void Deposit(float amount)
        {
            balance += amount;
        }

        // Retrage bani din cont
        public void Withdraw(float amount)
        {
            balance -= amount;
        }

        // Transfer normal intre doua conturi
        public void TransferFunds(Account destination, float amount)
        {
            destination.Deposit(amount);
            Withdraw(amount);
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

            // (5) Daca toate verificarile sunt trecute, se efectueaza transferul
            Withdraw(amount);
            destination.Deposit(amount);

            // Optional: mesaj informativ in consola (pentru debugging)
            Console.WriteLine($"Transfer reusit: {amount} lei trimisi. Sold ramas: {remainingBalance} lei.");

            return destination;
        }

        // Proprietati pentru accesarea valorilor curente
        public float Balance => balance;
        public float MinBalance => minBalance;
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
}