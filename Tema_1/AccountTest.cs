using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace bank
{
    [TestFixture]
    public class AccountTest
    {
        Account source;
        Account destination;

        [SetUp]
        public void InitAccount()
        {
            // Pregateste doua conturi pentru fiecare test
            // Valori mai mari pentru a testa scenarii realiste
            source = new Account();
            source.Deposit(500.00F);  // cont sursa cu 500 lei
            destination = new Account();
            destination.Deposit(450.00F); // cont destinatie cu 450 lei
        }

        // =====================================================================
        // TESTE CARE TREBUIE SA TREACA (PASS)
        // =====================================================================
        // Acestea simuleaza situatii valide de transfer, unde toate regulile
        // sunt respectate: suma este pozitiva, ramane minim 10 lei in cont,
        // si nu se depaseste 40% din soldul curent.
        // =====================================================================

        [Test]
        [Category("pass")]
        [TestCase(500, 450, 1, TestName = "Transfer minim valid (1 leu)")]
        [TestCase(500, 450, 50, TestName = "Transfer normal valid (10% din sold)")]
        [TestCase(500, 450, 150, TestName = "Transfer mediu valid (30% din sold)")]
        [TestCase(500, 450, 190, TestName = "Transfer 190 din 500 valid (38%)")]
        [TestCase(500, 450, 200, TestName = "Transfer fix 40% din sold (limita superioara)")]
        public void TransferMinFunds_PassCases(int a, int b, float c)
        {
            // Arrange: creeaza doua conturi cu sumele initiale
            Account source = new Account();
            source.Deposit(a);
            Account destination = new Account();
            destination.Deposit(b);

            // Act: realizeaza transferul
            source.TransferMinFunds(destination, c);

            // Assert: verifica daca destinatia a primit corect suma
            Assert.AreEqual(b + c, destination.Balance);
        }

        // =====================================================================
        // TESTE CARE TREBUIE SA DEA EROARE (FAIL)
        // =====================================================================
        // Aceste cazuri verifica daca metoda arunca corect exceptia
        // NotEnoughFundsException atunci cand regulile sunt incalcate.
        // =====================================================================

        [Test]
        [Category("fail")]
        // 1️⃣ Suma negativa
        [TestCase(500, 450, -1, TestName = "Transfer negativ (suma < 0)")]
        // 2️⃣ Suma zero
        [TestCase(500, 450, 0, TestName = "Transfer zero (suma = 0)")]
        // 3️⃣ Suma mai mare decat soldul
        [TestCase(500, 450, 600, TestName = "Transfer mai mare decat soldul (fonduri insuficiente)")]
        // 4️⃣ Transfer care lasa contul sub MinBalance (ramane < 10 lei)
        [TestCase(500, 450, 495, TestName = "Transfer lasa contul cu 5 lei (sub MinBalance)")]
        // 5️⃣ Transfer prea mare (>40% din sold)
        [TestCase(500, 450, 250, TestName = "Transfer prea mare (peste 40% din sold)")]
        // 6️⃣ Transfer exact toti banii (ramane 0)
        [TestCase(500, 450, 500, TestName = "Transfer toti banii (ramane 0 lei)")]
        // 7️⃣ Transfer care ramane fix la 9.99 (test de precizie float)
        [TestCase(500, 450, 490.01f, TestName = "Transfer lasa contul cu 9.99 lei (precizie float)")]
        // 8️⃣ Transfer din cont gol
        [TestCase(0, 450, 10, TestName = "Transfer din cont gol (sold zero)")]
        // 9️⃣ Transfer foarte mare din cont mic
        [TestCase(50, 450, 45, TestName = "Transfer lasa 5 lei (sub limita minima)")]
        // 🔟 Transfer de tip edge-case combinat
        [TestCase(500, 450, 210, TestName = "Transfer 42% din sold (peste limita permisa)")]
        public void TransferMinFunds_FailCases(int a, int b, float c)
        {
            // Arrange: creeaza doua conturi cu valorile primite
            Account source = new Account();
            source.Deposit(a);

            Account destination = new Account();
            destination.Deposit(b);

            // Assert: ne asteptam ca metoda sa arunce o exceptie
            Assert.Throws<NotEnoughFundsException>(() =>
            {
                source.TransferMinFunds(destination, c);
            });
        }

        // =====================================================================
        // TEST SUPLIMENTAR - TRANSFER SIMPLU FARA RESTRICTII
        // =====================================================================
        // Acesta verifica logica de baza a metodei TransferFunds()
        // =====================================================================

        [Test]
        [Category("pass")]
        public void TransferFunds_Basic()
        {
            // Act
            source.TransferFunds(destination, 100.00F);

            // Assert
            Assert.AreEqual(550.00F, destination.Balance);
            Assert.AreEqual(400.00F, source.Balance);
        }

        // =====================================================================
        // TEST COMBINATORIAL - combinatii multiple de valori
        // =====================================================================
        // Acest test combina mai multe valori posibile pentru a verifica
        // robustetea functiei in fata variatiilor de sold si sume.
        // =====================================================================

        [Test]
        [Category("fail")]
        [Combinatorial]
        public void TransferMinFundsFail_Combinatorial(
            [Values(200, 500)] int a,
            [Values(0, 450)] int b,
            [Values(210, 250, 495)] float c) // valori mai mari de 40% sau sub MinBalance
        {
            Account source = new Account();
            source.Deposit(a);
            Account destination = new Account();
            destination.Deposit(b);

            Assert.Throws<NotEnoughFundsException>(() =>
            {
                source.TransferMinFunds(destination, c);
            });
        }
    }
}