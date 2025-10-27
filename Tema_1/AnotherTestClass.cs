using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace bank
{
    [TestFixture]
    public class AnotherTestClass
    {
        private Account account;

        [SetUp]
        public void Initialize()
        {
            // =====================================================================
            // Aceasta metoda se apeleaza automat inainte de fiecare test.
            // Creeaza un cont nou cu 500 lei pentru a porni dintr-o situatie realista.
            // =====================================================================
            account = new Account(500);
            Console.WriteLine("Setup completat - cont initializat cu 500 lei.");
        }

        // =====================================================================
        // TEST 1: Verifica daca constructorul seteaza corect soldul initial
        // =====================================================================
        [Test]
        public void VerifyIfConstructorWorksOk()
        {
            // Actiune: afisam starea initiala a contului
            Console.WriteLine("Test: Verificare constructor.");

            // Verificare: soldul trebuie sa fie exact 500 (valoarea din constructor)
            Assert.AreEqual(500, account.Balance);

            Console.WriteLine("✅ Constructorul a setat corect soldul initial (500 lei).");
        }

        // =====================================================================
        // TEST 2: Verifica scaderea corecta a sumei din contul sursa
        // =====================================================================
        [Test]
        public void VerifyTransferFundsInSource()
        {
            // Arrange
            var account1 = new Account(700);
            var account2 = new Account(300);
            Console.WriteLine("Test: Verificare scadere din contul sursa.");

            // Act
            account1.TransferFunds(account2, 200);

            // Assert
            // Dupa transfer, contul sursa (account1) trebuie sa aiba 500 lei
            Assert.AreEqual(500, account1.Balance);

            Console.WriteLine("✅ Transferul a scazut corect suma din contul sursa (700 - 200 = 500).");
        }

        // =====================================================================
        // TEST 3: Verifica cresterea corecta a sumei in contul destinatie
        // =====================================================================
        [Test]
        public void VerifyTransferFundsInDestination()
        {
            // Arrange
            var account1 = new Account(700);
            var account2 = new Account(300);
            Console.WriteLine("Test: Verificare crestere in contul destinatie.");

            // Act
            account1.TransferFunds(account2, 200);

            // Assert
            // Dupa transfer, contul destinatie (account2) trebuie sa aiba 500 lei
            Assert.AreEqual(500, account2.Balance);

            Console.WriteLine("✅ Transferul a crescut corect suma in contul destinatie (300 + 200 = 500).");
        }

        // =====================================================================
        // TEST 4: Verifica transferul cand suma este zero
        // =====================================================================
        // Acest test demonstreaza ca metoda TransferFunds functioneaza logic,
        // dar in practica s-ar putea impune interzicerea sumelor nule.
        // =====================================================================
        [Test]
        public void VerifyTransferZeroAmount()
        {
            var account1 = new Account(500);
            var account2 = new Account(700);
            Console.WriteLine("Test: Transfer de suma zero.");

            // Act
            account1.TransferFunds(account2, 0);

            // Assert
            // Soldurile raman neschimbate
            Assert.AreEqual(500, account1.Balance);
            Assert.AreEqual(700, account2.Balance);

            Console.WriteLine("✅ Transferul cu suma 0 nu a modificat soldurile (comportament corect).");
        }

        // =====================================================================
        // TEST 5: Verifica transferul unei sume negative
        // =====================================================================
        // In mod normal, o astfel de operatie ar trebui blocata.
        // Acest test confirma comportamentul actual (fara validare),
        // pentru a evidentia ca TransferFunds permite valori negative.
        // =====================================================================
        [Test]
        public void VerifyTransferNegativeAmount()
        {
            var account1 = new Account(500);
            var account2 = new Account(700);
            Console.WriteLine("Test: Transfer negativ (suma < 0).");

            // Act
            account1.TransferFunds(account2, -100);

            // Assert
            // Dupa transfer, sursa primeste 100, destinatia pierde 100
            Assert.AreEqual(600, account1.Balance);
            Assert.AreEqual(600, account2.Balance);

            Console.WriteLine("⚠️ Transferul negativ a inversat direct sumele (comportament nedorit, dar detectat).");
        }
    }
}