using System;
using System.Collections.Generic;
using System.Text;
using Moq;
using NUnit.Framework;

namespace bank
{
    [TestFixture]
    public class AccountTest
    {
        // Constantă pentru curs valutar - schimbi DOAR aici pentru toate testele!
        private const float TEST_EUR_TO_RON_RATE = 5.0f;

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
            Assert.That(destination.Balance, Is.EqualTo(b + c));
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
            Assert.That(destination.Balance, Is.EqualTo(550.00F));
            Assert.That(source.Balance, Is.EqualTo(400.00F));
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

        // =====================================================================
        // TEST: Conversie valutara folosind stub (RON -> EUR)
        // =====================================================================
        [Test]
        public void TransferCurrency_RonToEur_UsingStub()
        {
            // Aranjare: sursa are 1000 RON, destinatia este in EUR
            Account source = new Account();
            source.Deposit(1000.00F);
            Account destination = new Account(0, "EUR");

            // Stub: 1 EUR = TEST_EUR_TO_RON_RATE RON
            var stub = new CurrencyConverterStub(TEST_EUR_TO_RON_RATE);

            // Actiune: transfer 400 RON (40% din 1000 -> permis)
            source.TransferCurrency(destination, 400.00F, "EUR", stub);

            // Verificare: destinatia ar trebui sa primeasca 400 / 5 = 80 EUR
            Assert.That(destination.Balance, Is.EqualTo(80.00F));
            // Sursa ar trebui sa scada cu 400 RON
            Assert.That(source.Balance, Is.EqualTo(600.00F));
        }

        // =====================================================================
        // TEST: Conversie valutara folosind stub (EUR -> RON)
        // =====================================================================
        [Test]
        public void TransferCurrency_EurToRon_UsingStub()
        {
            // Aranjare: sursa are 200 EUR, destinatia este in RON
            Account source = new Account(0, "EUR");
            source.Deposit(200.00F);
            Account destination = new Account(0, "RON");

            // Stub: 1 EUR = TEST_EUR_TO_RON_RATE RON
            var stub = new CurrencyConverterStub(TEST_EUR_TO_RON_RATE);

            // Actiune: transfer 80 EUR (40% din 200 EUR -> permis)
            source.TransferCurrency(destination, 80.00F, "RON", stub);

            // Verificare: destinatia ar trebui sa primeasca 80 * 5 = 400 RON
            Assert.That(destination.Balance, Is.EqualTo(400.00F));
            // Sursa ar trebui sa scada cu 80 EUR
            Assert.That(source.Balance, Is.EqualTo(120.00F));
        }

        // =====================================================================
        // TESTE CURRENCY CONVERTER cu STUB
        // =====================================================================

        // Test 11: Verifica conversia RON -> EUR cu curs fix (STUB)
        [Test, Category("pass")]
        [Description("Testeaza conversie RON la EUR folosind stub cu curs fix")]
        public void ConvertRonToEur_WithStub_ShouldCalculateCorrectly()
        {
            // Cream un stub cu curs fix: 1 EUR = TEST_EUR_TO_RON_RATE RON
            var stubConverter = new CurrencyConverterStub(TEST_EUR_TO_RON_RATE);
            var account = new Account(1000, stubConverter);

            // Convertim 100 RON la EUR
            // 100 RON / TEST_EUR_TO_RON_RATE = rezultat EUR
            float result = account.ConvertRonToEur(100);

            Assert.That(result, Is.EqualTo(20.0f).Within(0.01f), $"100 RON trebuie sa fie 20 EUR la curs {TEST_EUR_TO_RON_RATE}");
            Assert.Pass("Conversia RON->EUR cu stub a reusit");
        }

        // Test 12: Verifica conversia EUR -> RON cu curs fix (STUB)
        [Test, Category("pass")]
        [Description("Testeaza conversie EUR la RON folosind stub cu curs fix")]
        public void ConvertEurToRon_WithStub_ShouldCalculateCorrectly()
        {
            // Cream un stub cu curs fix: 1 EUR = TEST_EUR_TO_RON_RATE RON
            var stubConverter = new CurrencyConverterStub(TEST_EUR_TO_RON_RATE);
            var account = new Account(1000, stubConverter);

            // Convertim 20 EUR la RON
            // 20 EUR * TEST_EUR_TO_RON_RATE = rezultat RON
            float result = account.ConvertEurToRon(20);

            Assert.That(result, Is.EqualTo(100.0f).Within(0.01f), $"20 EUR trebuie sa fie 100 RON la curs {TEST_EUR_TO_RON_RATE}");
            Assert.Pass("Conversia EUR->RON cu stub a reusit");
        }

        // Test 13: Transfer international RON -> EUR cu STUB
        [Test, Category("pass")]
        [Description("Testeaza transfer international RON->EUR folosind stub")]
        public void TransferRonToEur_WithStub_ShouldTransferCorrectAmount()
        {
            // Cream stub cu curs fix: 1 EUR = TEST_EUR_TO_RON_RATE RON
            var stubConverter = new CurrencyConverterStub(TEST_EUR_TO_RON_RATE);
            
            // Cont sursa: 1000 RON
            var sourceAccount = new Account(1000, stubConverter);
            // Cont destinatie: 0 EUR
            var destinationAccount = new Account(0, stubConverter);

            // Transferam 500 RON -> EUR
            // 500 RON / TEST_EUR_TO_RON_RATE = rezultat EUR
            sourceAccount.TransferRonToEur(destinationAccount, 500);

            // Verificam: sursa a ramas cu 500 RON
            Assert.That(sourceAccount.Balance, Is.EqualTo(500).Within(0.01f), 
                "Sursa trebuie sa aiba 500 RON ramas");
            // Verificam: destinatia a primit 100 EUR
            Assert.That(destinationAccount.Balance, Is.EqualTo(100).Within(0.01f), 
                "Destinatia trebuie sa aiba 100 EUR");
            Assert.Pass("Transferul RON->EUR cu stub a reusit");
        }

        // Test 14: Transfer international EUR -> RON cu STUB
        [Test, Category("pass")]
        [Description("Testeaza transfer international EUR->RON folosind stub")]
        public void TransferEurToRon_WithStub_ShouldTransferCorrectAmount()
        {
            // Cream stub cu curs fix: 1 EUR = TEST_EUR_TO_RON_RATE RON
            var stubConverter = new CurrencyConverterStub(TEST_EUR_TO_RON_RATE);
            
            // Cont sursa: 100 EUR
            var sourceAccount = new Account(100, stubConverter);
            // Cont destinatie: 0 RON
            var destinationAccount = new Account(0, stubConverter);

            // Transferam 50 EUR -> RON
            // 50 EUR * TEST_EUR_TO_RON_RATE = rezultat RON
            sourceAccount.TransferEurToRon(destinationAccount, 50);

            // Verificam: sursa a ramas cu 50 EUR
            Assert.That(sourceAccount.Balance, Is.EqualTo(50).Within(0.01f), 
                "Sursa trebuie sa aiba 50 EUR ramas");
            // Verificam: destinatia a primit 250 RON
            Assert.That(destinationAccount.Balance, Is.EqualTo(250).Within(0.01f), 
                "Destinatia trebuie sa aiba 250 RON");
            Assert.Pass("Transferul EUR->RON cu stub a reusit");
        }

        // Test 15: Teste cu CURSURI DIFERITE - demonstreaza flexibilitatea STUB-ului
        [TestCase(4.5f, 450, 100)]   // Curs 4.5: 450 RON = 100 EUR
        [TestCase(5.0f, 500, 100)]   // Curs 5.0: 500 RON = 100 EUR
        [TestCase(4.97f, 497, 100)]  // Curs 4.97 (BNR real): 497 RON ≈ 100 EUR
        [Category("pass")]
        [Description("Testeaza conversii cu cursuri diferite folosind stub parametrizat")]
        public void ConvertRonToEur_WithDifferentRates_ShouldCalculateCorrectly(float rate, float ron, float expectedEur)
        {
            // Cream stub cu cursul specificat
            var stubConverter = new CurrencyConverterStub(rate);
            var account = new Account(1000, stubConverter);

            // Convertim RON la EUR
            float result = account.ConvertRonToEur(ron);

            // Verificam rezultatul (cu toleranta de 0.1 pentru erori de rotunjire)
            Assert.That(result, Is.EqualTo(expectedEur).Within(0.1f), 
                $"{ron} RON trebuie sa fie aproximativ {expectedEur} EUR la curs {rate}");
            Assert.Pass($"Conversia cu curs {rate} a reusit");
        }

        // Test 16: Verifica ca nu se poate transfera o suma negativa
        [Test, Category("pass")]
        [Description("Testeaza rejectia sumelor negative la conversie")]
        public void ConvertRonToEur_NegativeAmount_ShouldThrow()
        {
            var stubConverter = new CurrencyConverterStub(TEST_EUR_TO_RON_RATE);
            var account = new Account(1000, stubConverter);

            Assert.Throws<ArgumentException>(() => account.ConvertRonToEur(-100), 
                "Trebuie sa arunce ArgumentException pentru suma negativa");
            Assert.Pass("Exceptia pentru suma negativa a fost aruncata corect");
        }

        // =====================================================================
        // TESTE PENTRU FUNCȚIONALITĂȚILE NOI
        // =====================================================================

        // ============== TESTE ISTORIC TRANZACȚII ==============
        [Test, Category("pass")]
        [Description("Verifica ca istoricul de tranzactii inregistreaza corect depunerile")]
        public void TransactionHistory_RecordsDeposits_Correctly()
        {
            // Arrange
            Account account = new Account();
            
            // Act
            account.Deposit(100);
            account.Deposit(200);
            account.Deposit(50);
            
            var history = account.GetTransactionHistory();
            
            // Assert
            Assert.That(history.Count, Is.EqualTo(3), "Ar trebui sa existe 3 tranzactii");
            Assert.That(history[0].Amount, Is.EqualTo(100));
            Assert.That(history[1].Amount, Is.EqualTo(200));
            Assert.That(history[2].Amount, Is.EqualTo(50));
            Assert.That(history[2].BalanceAfter, Is.EqualTo(350));
        }

        [Test, Category("pass")]
        [Description("Verifica ca istoricul inregistreaza atat depuneri cat si retrageri")]
        public void TransactionHistory_RecordsMixedTransactions_Correctly()
        {
            // Arrange
            Account account = new Account();
            account.Deposit(1000);
            
            // Act
            account.Withdraw(200);
            account.Deposit(300);
            account.Withdraw(100);
            
            var history = account.GetTransactionHistory();
            
            // Assert
            Assert.That(history.Count, Is.EqualTo(4), "4 tranzactii in total");
            Assert.That(history[1].Type, Is.EqualTo("Withdraw"));
            Assert.That(history[2].Type, Is.EqualTo("Deposit"));
            Assert.That(account.Balance, Is.EqualTo(1000));
        }

        // ============== TESTE BLOCARE CONT ==============
        [Test, Category("pass")]
        [Description("Verifica ca un cont blocat nu permite depuneri")]
        public void LockedAccount_PreventDeposit_ThrowsException()
        {
            // Arrange
            Account account = new Account();
            account.Deposit(500);
            account.LockAccount(1); // Blocat pentru 1 ora
            
            // Act & Assert
            Assert.Throws<bank.AccountLockedException>(() => account.Deposit(100));
        }

        [Test, Category("pass")]
        [Description("Verifica ca un cont blocat nu permite retrageri")]
        public void LockedAccount_PreventWithdraw_ThrowsException()
        {
            // Arrange
            Account account = new Account();
            account.Deposit(500);
            account.LockAccount(2); // Blocat pentru 2 ore
            
            // Act & Assert
            Assert.Throws<bank.AccountLockedException>(() => account.Withdraw(50));
        }

        [Test, Category("pass")]
        [Description("Verifica ca deblocarea contului permite din nou operatiuni")]
        public void UnlockedAccount_AllowsOperations_Successfully()
        {
            // Arrange
            Account account = new Account();
            account.Deposit(500);
            account.LockAccount(1);
            
            // Act
            account.UnlockAccount();
            account.Deposit(100);
            
            // Assert
            Assert.That(account.Balance, Is.EqualTo(600));
            Assert.That(account.IsLocked, Is.False);
        }

        // ============== TESTE LIMITĂ ZILNICĂ ==============
        [Test, Category("pass")]
        [Description("Verifica ca limita zilnica previne transferuri prea mari")]
        public void DailyLimit_PreventExcessiveTransfers_ThrowsException()
        {
            // Arrange
            Account source = new Account();
            source.Deposit(10000);
            source.SetDailyLimit(1000); // Limita de 1000 RON/zi
            
            Account destination = new Account();
            destination.Deposit(100);
            
            // Act & Assert - primul transfer de 600 RON e OK
            source.TransferMinFunds(destination, 600);
            
            // Al doilea transfer de 500 RON ar depasi limita (600+500=1100 > 1000)
            Assert.Throws<bank.DailyLimitExceededException>(() => 
                source.TransferMinFunds(destination, 500));
        }

        [Test, Category("pass")]
        [Description("Verifica ca limita zilnica permite transferuri sub limita")]
        public void DailyLimit_AllowsTransfersUnderLimit_Successfully()
        {
            // Arrange
            Account source = new Account();
            source.Deposit(5000);
            source.SetDailyLimit(2000);
            
            Account destination = new Account();
            destination.Deposit(100);
            
            // Act - 3 transferuri mici care nu depasesc limita
            source.TransferMinFunds(destination, 500);
            source.TransferMinFunds(destination, 400);
            source.TransferMinFunds(destination, 300);
            
            // Assert
            Assert.That(source.TotalTransferredToday, Is.EqualTo(1200));
            Assert.That(destination.Balance, Is.EqualTo(1300));
        }

        [Test, Category("pass")]
        [Description("Verifica setarea unei noi limite zilnice")]
        public void SetDailyLimit_UpdatesLimit_Successfully()
        {
            // Arrange
            Account account = new Account();
            
            // Act
            account.SetDailyLimit(3000);
            
            // Assert
            Assert.That(account.DailyTransferLimit, Is.EqualTo(3000));
        }

        // ============== TESTE DOBÂNDĂ ==============
        [Test, Category("pass")]
        [Description("Verifica setarea ratei dobanzii")]
        public void SetInterestRate_UpdatesRate_Successfully()
        {
            // Arrange
            Account account = new Account();
            account.Deposit(1000);
            
            // Act
            account.SetInterestRate(0.05f); // 5%
            
            // Assert
            Assert.That(account.InterestRate, Is.EqualTo(0.05f));
        }

        [Test, Category("pass")]
        [Description("Verifica ca rata dobanzii invalida arunca exceptie")]
        public void SetInterestRate_InvalidRate_ThrowsException()
        {
            // Arrange
            Account account = new Account();
            
            // Act & Assert
            Assert.Throws<ArgumentException>(() => account.SetInterestRate(-0.01f));
            Assert.Throws<ArgumentException>(() => account.SetInterestRate(1.5f));
        }

        // ============== TESTE STATISTICI CONT ==============
        [Test, Category("pass")]
        [Description("Verifica calculul statisticilor contului")]
        public void GetStatistics_CalculatesCorrectly()
        {
            // Arrange
            Account account = new Account();
            account.Deposit(500);
            account.Deposit(300);
            account.Deposit(200);
            account.Withdraw(150);
            account.Withdraw(50);
            
            // Act
            var stats = account.GetStatistics();
            
            // Assert
            Assert.That(stats.TotalDeposits, Is.EqualTo(1000));
            Assert.That(stats.TotalWithdrawals, Is.EqualTo(200));
            Assert.That(stats.DepositCount, Is.EqualTo(3));
            Assert.That(stats.WithdrawalCount, Is.EqualTo(2));
            Assert.That(stats.TransactionCount, Is.EqualTo(5));
        }

        [Test, Category("pass")]
        [Description("Verifica calculul mediilor in statistici")]
        public void GetStatistics_CalculatesAverages_Correctly()
        {
            // Arrange
            Account account = new Account();
            account.Deposit(100);
            account.Deposit(200);
            account.Deposit(300);
            account.Withdraw(60);
            account.Withdraw(90);
            
            // Act
            var stats = account.GetStatistics();
            
            // Assert
            Assert.That(stats.AverageDeposit, Is.EqualTo(200).Within(0.01f));
            Assert.That(stats.AverageWithdrawal, Is.EqualTo(75).Within(0.01f));
        }

        // ============== TESTE VERIFICARE SECURITATE ==============
        [Test, Category("pass")]
        [Description("Verifica ca un cont valid este considerat sigur")]
        public void IsAccountSecure_ValidAccount_ReturnsTrue()
        {
            // Arrange
            Account account = new Account();
            account.Deposit(500);
            
            // Act
            bool isSecure = account.IsAccountSecure();
            
            // Assert
            Assert.That(isSecure, Is.True);
        }

        [Test, Category("pass")]
        [Description("Verifica ca un cont blocat nu este considerat sigur")]
        public void IsAccountSecure_LockedAccount_ReturnsFalse()
        {
            // Arrange
            Account account = new Account();
            account.Deposit(500);
            account.LockAccount(1);
            
            // Act
            bool isSecure = account.IsAccountSecure();
            
            // Assert
            Assert.That(isSecure, Is.False);
        }

        // ============== TESTE COMBINAȚII COMPLEXE ==============
        [Test, Category("pass")]
        [Description("Test complex: transferuri multiple cu verificare istoric si statistici")]
        public void ComplexScenario_MultipleTransfers_WithHistoryAndStats()
        {
            // Arrange
            Account source = new Account();
            source.Deposit(2000);
            source.SetDailyLimit(1500);
            
            Account destination = new Account();
            destination.Deposit(500);
            
            // Act
            source.TransferMinFunds(destination, 300);
            source.TransferMinFunds(destination, 200);
            source.Deposit(500);
            source.TransferMinFunds(destination, 100);
            
            // Assert - Verificare solduri
            Assert.That(source.Balance, Is.EqualTo(1900));
            Assert.That(destination.Balance, Is.EqualTo(1100));
            
            // Assert - Verificare istoric
            var sourceHistory = source.GetTransactionHistory();
            Assert.That(sourceHistory.Count, Is.GreaterThan(4));
            
            // Assert - Verificare statistici
            var stats = destination.GetStatistics();
            Assert.That(stats.TotalDeposits, Is.EqualTo(1100));
        }

        [Test, Category("pass")]
        [Description("Test combinat: blocare temporara apoi deblocare si continuare operatiuni")]
        public void ComplexScenario_LockUnlockAndContinue()
        {
            // Arrange
            Account account = new Account();
            account.Deposit(1000);
            
            // Act - Operatiuni normale
            account.Withdraw(100);
            Assert.That(account.Balance, Is.EqualTo(900));
            
            // Blocare
            account.LockAccount(1);
            Assert.Throws<bank.AccountLockedException>(() => account.Deposit(50));
            
            // Deblocare
            account.UnlockAccount();
            account.Deposit(200);
            
            // Assert
            Assert.That(account.Balance, Is.EqualTo(1100));
            Assert.That(account.IsLocked, Is.False);
        }

        [Test, Category("pass")]
        [Description("Test edge case: transfer exact la limita de 40% cu sold mare")]
        public void EdgeCase_ExactFourtyPercentTransfer_LargeBalance()
        {
            // Arrange
            Account source = new Account();
            source.Deposit(10000);
            Account destination = new Account();
            destination.Deposit(1000);
            
            // Act - Transfer exact 40% din 10000 = 4000
            source.TransferMinFunds(destination, 4000);
            
            // Assert
            Assert.That(source.Balance, Is.EqualTo(6000));
            Assert.That(destination.Balance, Is.EqualTo(5000));
        }

        [Test, Category("pass")]
        [Description("Test: operatiuni multiple pe cont gol initial")]
        public void EmptyAccount_MultipleOperations_WorksCorrectly()
        {
            // Arrange
            Account account = new Account();
            
            // Act
            account.Deposit(50);
            account.Deposit(100);
            account.Withdraw(30);
            
            // Assert
            Assert.That(account.Balance, Is.EqualTo(120));
            var history = account.GetTransactionHistory();
            Assert.That(history.Count, Is.EqualTo(3));
        }

        // =====================================================================
        // ========== TESTE MOCK ==========
        // =====================================================================
        // Mock Object = un obiect de test care simuleaza comportamentul unei dependente externe
        // Si permite VERIFICAREA ca metodele au fost apelate corect (spre deosebire de STUB)
        
        [Test, Category("pass")]
        [Description("Mock: Verifica ca metoda GetEurToRonRate() este apelata exact o data")]
        public void ConvertRonToEur_CallsGetEurToRonRate_ExactlyOnce()
        {
            // Arrange
            var mockConverter = new CurrencyConverterMock(TEST_EUR_TO_RON_RATE);
            var account = new Account(1000, mockConverter);
            
            // Act
            account.ConvertRonToEur(100);
            
            // Assert - MOCK verifica ca metoda a fost apelata
            Assert.That(mockConverter.GetRateCallCount, Is.EqualTo(1), 
                "GetEurToRonRate() trebuie apelata exact o data");
        }

        [Test, Category("pass")]
        [Description("Mock: Verifica ca GetEurToRonRate() este apelata de doua ori pentru doua conversii")]
        public void MultipleConversions_CallsGetEurToRonRate_MultipleTimes()
        {
            // Arrange
            var mockConverter = new CurrencyConverterMock(TEST_EUR_TO_RON_RATE);
            var account = new Account(1000, mockConverter);
            
            // Act
            account.ConvertRonToEur(100);
            account.ConvertEurToRon(20);
            
            // Assert - MOCK verifica ca metoda a fost apelata de 2 ori
            Assert.That(mockConverter.GetRateCallCount, Is.EqualTo(2), 
                "GetEurToRonRate() trebuie apelata de 2 ori");
        }

        [Test, Category("pass")]
        [Description("Mock: Verifica ca transferul valutar apeleaza conversia o singura data")]
        public void TransferRonToEur_UsingMock_CallsGetRateOnce()
        {
            // Arrange
            var mockConverter = new CurrencyConverterMock(TEST_EUR_TO_RON_RATE);
            var sourceAccount = new Account(1000, mockConverter);
            var destinationAccount = new Account(0, mockConverter);
            
            // Act
            sourceAccount.TransferRonToEur(destinationAccount, 500);
            
            // Assert - Verificam ca metoda a fost apelata
            Assert.That(mockConverter.GetRateCallCount, Is.GreaterThanOrEqualTo(1), 
                "GetEurToRonRate() trebuie apelata cel putin o data");
            
            // Verificam si rezultatele
            Assert.That(destinationAccount.Balance, Is.EqualTo(100).Within(0.01f));
        }

        [Test, Category("pass")]
        [Description("Mock: Verifica comportamentul cu rate dinamice")]
        public void ConvertWithDynamicRate_UsingMock_TracksAllCalls()
        {
            // Arrange - Mock cu rate dinamice
            var mockConverter = new CurrencyConverterMock(5.0f);
            var account = new Account(1000, mockConverter);
            
            // Act - Prima conversie
            float result1 = account.ConvertRonToEur(500);
            
            // Schimbam cursul in mock
            mockConverter.SetRate(4.5f);
            
            // A doua conversie cu curs diferit
            float result2 = account.ConvertRonToEur(450);
            
            // Assert
            Assert.That(mockConverter.GetRateCallCount, Is.EqualTo(2));
            Assert.That(result1, Is.EqualTo(100).Within(0.01f)); // 500/5 = 100
            Assert.That(result2, Is.EqualTo(100).Within(0.01f)); // 450/4.5 = 100
        }

        [Test, Category("pass")]
        [Description("Mock: Verifica ca nu se apeleaza GetEurToRonRate() pentru sume negative")]
        public void ConvertNegativeAmount_UsingMock_DoesNotCallGetRate()
        {
            // Arrange
            var mockConverter = new CurrencyConverterMock(TEST_EUR_TO_RON_RATE);
            var account = new Account(1000, mockConverter);
            
            // Act & Assert - Exceptie aruncata inainte de a apela GetRate
            Assert.Throws<ArgumentException>(() => account.ConvertRonToEur(-100));
            
            // Mock verifica ca GetRate() NU a fost apelata
            Assert.That(mockConverter.GetRateCallCount, Is.EqualTo(0), 
                "GetEurToRonRate() NU trebuie apelata pentru sume negative");
        }

        [Test, Category("pass")]
        [Description("Mock: Verifica ordinea apelurilor pentru transfer bidirectional")]
        public void BidirectionalTransfer_UsingMock_TracksCallOrder()
        {
            // Arrange
            var mockConverter = new CurrencyConverterMock(TEST_EUR_TO_RON_RATE);
            var account = new Account(1000, mockConverter);
            
            // Act - Conversie RON -> EUR
            account.ConvertRonToEur(100);
            int callsAfterFirstConversion = mockConverter.GetRateCallCount;
            
            // Conversie EUR -> RON
            account.ConvertEurToRon(20);
            int callsAfterSecondConversion = mockConverter.GetRateCallCount;
            
            // Assert - Verificam ca fiecare conversie a apelat GetRate()
            Assert.That(callsAfterFirstConversion, Is.EqualTo(1));
            Assert.That(callsAfterSecondConversion, Is.EqualTo(2));
        }

        [Test, Category("pass")]
        [Description("Mock: Verifica ca mock-ul poate inregistra parametrii apelurilor")]
        public void Mock_RecordsCallParameters_Successfully()
        {
            // Arrange
            var mockConverter = new CurrencyConverterMock(TEST_EUR_TO_RON_RATE);
            var account = new Account(1000, mockConverter);
            
            // Act
            account.ConvertRonToEur(100);
            account.ConvertRonToEur(200);
            account.ConvertEurToRon(50);
            
            // Assert
            Assert.That(mockConverter.GetRateCallCount, Is.EqualTo(3));
            Assert.That(mockConverter.WasCalled, Is.True);
        }

        [Test, Category("pass")]
        [Description("Mock: Verifica resetarea starii mock-ului")]
        public void Mock_CanBeReset_Successfully()
        {
            // Arrange
            var mockConverter = new CurrencyConverterMock(TEST_EUR_TO_RON_RATE);
            var account = new Account(1000, mockConverter);
            
            // Act - Prima serie de apeluri
            account.ConvertRonToEur(100);
            account.ConvertRonToEur(200);
            Assert.That(mockConverter.GetRateCallCount, Is.EqualTo(2));
            
            // Reset mock
            mockConverter.Reset();
            
            // A doua serie de apeluri
            account.ConvertRonToEur(300);
            
            // Assert - Counter resetat
            Assert.That(mockConverter.GetRateCallCount, Is.EqualTo(1));
        }

        [Test, Category("pass")]
        [Description("Mock: Scenario complex cu verificare multipla")]
        public void ComplexScenario_WithMock_VerifiesAllInteractions()
        {
            // Arrange
            var mockConverter = new CurrencyConverterMock(TEST_EUR_TO_RON_RATE);
            var sourceAccount = new Account(2000, mockConverter);
            var destinationAccount = new Account(500, mockConverter);
            
            // Act
            sourceAccount.TransferRonToEur(destinationAccount, 500); // 1 apel
            sourceAccount.ConvertRonToEur(100);                      // 1 apel
            sourceAccount.ConvertEurToRon(20);                       // 1 apel
            
            // Assert
            Assert.That(mockConverter.GetRateCallCount, Is.GreaterThanOrEqualTo(3), 
                "Trebuie sa fie cel putin 3 apeluri");
            Assert.That(mockConverter.WasCalled, Is.True);
            Assert.That(sourceAccount.Balance, Is.EqualTo(1500).Within(0.01f));
        }

        [Test, Category("pass")]
        [Description("Mock vs Stub: Demonstreaza diferenta dintre Mock si Stub")]
        public void MockVsStub_DemonstrateDifference()
        {
            // STUB - Doar returneaza valori predefinite
            var stub = new CurrencyConverterStub(TEST_EUR_TO_RON_RATE);
            var accountWithStub = new Account(1000, stub);
            accountWithStub.ConvertRonToEur(100);
            // Nu putem verifica cate ori a fost apelat - STUB nu tine evidenta
            
            // MOCK - Tine evidenta apelurilor si permite verificari
            var mock = new CurrencyConverterMock(TEST_EUR_TO_RON_RATE);
            var accountWithMock = new Account(1000, mock);
            accountWithMock.ConvertRonToEur(100);
            
            // Assert - MOCK permite verificari despre apeluri
            Assert.That(mock.GetRateCallCount, Is.EqualTo(1), 
                "MOCK-ul permite verificarea numarului de apeluri");
            Assert.That(mock.WasCalled, Is.True, 
                "MOCK-ul poate confirma ca metoda a fost apelata");
        }

        // =====================================================================
        // ========== TESTE CU MOQ FRAMEWORK ==========
        // =====================================================================
        // Moq = biblioteca profesionala pentru crearea de mock objects in C#
        // Avantaje: sintaxa fluent, verificari puternice, setup configurabil
        // =====================================================================

        [Test, Category("pass")]
        [Description("Moq: Verifica apelul GetEurToRonRate() folosind Moq framework")]
        public void ConvertRonToEur_UsingMoq_VerifiesMethodCall()
        {
            // Arrange - Cream un mock folosind Moq
            var mockConverter = new Mock<ICurrencyConverter>();
            mockConverter.Setup(x => x.GetEurToRonRate()).Returns(TEST_EUR_TO_RON_RATE);
            
            var account = new Account(1000, mockConverter.Object);
            
            // Act
            float result = account.ConvertRonToEur(100);
            
            // Assert - Verificam rezultatul
            Assert.That(result, Is.EqualTo(20.0f).Within(0.01f));
            
            // Assert - Verificam ca metoda a fost apelata exact o data
            mockConverter.Verify(x => x.GetEurToRonRate(), Times.Once);
        }

        [Test, Category("pass")]
        [Description("Moq: Verifica ca metoda NU este apelata pentru sume negative")]
        public void ConvertNegativeAmount_UsingMoq_VerifiesNoCall()
        {
            // Arrange
            var mockConverter = new Mock<ICurrencyConverter>();
            mockConverter.Setup(x => x.GetEurToRonRate()).Returns(TEST_EUR_TO_RON_RATE);
            
            var account = new Account(1000, mockConverter.Object);
            
            // Act & Assert - Exceptie aruncata
            Assert.Throws<ArgumentException>(() => account.ConvertRonToEur(-100));
            
            // Assert - Verificam ca GetEurToRonRate() NU a fost apelata deloc
            mockConverter.Verify(x => x.GetEurToRonRate(), Times.Never);
        }

        [Test, Category("pass")]
        [Description("Moq: Test complex cu verificari multiple")]
        public void ComplexScenario_UsingMoq_VerifiesAllInteractions()
        {
            // Arrange
            var mockConverter = new Mock<ICurrencyConverter>();
            mockConverter.Setup(x => x.GetEurToRonRate()).Returns(TEST_EUR_TO_RON_RATE);
            
            var sourceAccount = new Account(2000, mockConverter.Object);
            var destinationAccount = new Account(500, mockConverter.Object);
            
            // Act - Operatiuni complexe
            sourceAccount.TransferRonToEur(destinationAccount, 500);
            sourceAccount.ConvertRonToEur(100);
            sourceAccount.ConvertEurToRon(20);
            
            // Assert - Verificari multiple
            Assert.That(sourceAccount.Balance, Is.EqualTo(1500).Within(0.01f));
            mockConverter.Verify(x => x.GetEurToRonRate(), Times.AtLeast(3));
        }
    }
}