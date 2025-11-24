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
        // TESTE NORMALE - TransferMinFunds (4 teste)
        // =====================================================================

        [Test]
        [Category("pass")]
        [TestCase(500, 450, 50, TestName = "Transfer normal valid (10% din sold)")]
        [TestCase(500, 450, 200, TestName = "Transfer fix 40% din sold (limita superioara)")]
        public void TransferMinFunds_PassCases(int a, int b, float c)
        {
            Account source = new Account();
            source.Deposit(a);
            Account destination = new Account();
            destination.Deposit(b);

            source.TransferMinFunds(destination, c);

            Assert.That(destination.Balance, Is.EqualTo(b + c));
        }

        [Test]
        [Category("fail")]
        [TestCase(500, 450, 0, TestName = "Transfer zero (suma = 0)")]
        [TestCase(500, 450, 250, TestName = "Transfer prea mare (peste 40% din sold)")]
        public void TransferMinFunds_FailCases(int a, int b, float c)
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
        // TESTE CURRENCY CONVERTER cu STUB (5 teste)
        // =====================================================================

        // Test Stub 1: Verifica conversia RON -> EUR cu curs fix
        [Test, Category("pass")]
        [Description("Testeaza conversie RON la EUR folosind stub cu curs fix")]
        public void ConvertRonToEur_WithStub_ShouldCalculateCorrectly()
        {
            // Cream un stub cu curs fix: 1 EUR = TEST_EUR_TO_RON_RATE RON
            var stubConverter = new CurrencyConverterStub(TEST_EUR_TO_RON_RATE);
            var account = new Account(1000, stubConverter);

            // Convertim 100 RON la EUR
            float result = account.ConvertRonToEur(100);

            Assert.That(result, Is.EqualTo(20.0f).Within(0.01f), $"100 RON trebuie sa fie 20 EUR la curs {TEST_EUR_TO_RON_RATE}");
            Assert.Pass("Conversia RON->EUR cu stub a reusit");
        }

        // Test Stub 2: Verifica conversia EUR -> RON cu curs fix
        [Test, Category("pass")]
        [Description("Testeaza conversie EUR la RON folosind stub cu curs fix")]
        public void ConvertEurToRon_WithStub_ShouldCalculateCorrectly()
        {
            // Cream un stub cu curs fix: 1 EUR = TEST_EUR_TO_RON_RATE RON
            var stubConverter = new CurrencyConverterStub(TEST_EUR_TO_RON_RATE);
            var account = new Account(1000, stubConverter);

            // Convertim 20 EUR la RON
            float result = account.ConvertEurToRon(20);

            Assert.That(result, Is.EqualTo(100.0f).Within(0.01f), $"20 EUR trebuie sa fie 100 RON la curs {TEST_EUR_TO_RON_RATE}");
            Assert.Pass("Conversia EUR->RON cu stub a reusit");
        }

        // Test Stub 3: Transfer international RON -> EUR
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
            sourceAccount.TransferRonToEur(destinationAccount, 500);

            // Verificam: sursa a ramas cu 500 RON
            Assert.That(sourceAccount.Balance, Is.EqualTo(500).Within(0.01f), 
                "Sursa trebuie sa aiba 500 RON ramas");
            // Verificam: destinatia a primit 100 EUR
            Assert.That(destinationAccount.Balance, Is.EqualTo(100).Within(0.01f), 
                "Destinatia trebuie sa aiba 100 EUR");
            Assert.Pass("Transferul RON->EUR cu stub a reusit");
        }

        // Test Stub 4: Transfer international EUR -> RON
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
            sourceAccount.TransferEurToRon(destinationAccount, 50);

            // Verificam: sursa a ramas cu 50 EUR
            Assert.That(sourceAccount.Balance, Is.EqualTo(50).Within(0.01f), 
                "Sursa trebuie sa aiba 50 EUR ramas");
            // Verificam: destinatia a primit 250 RON
            Assert.That(destinationAccount.Balance, Is.EqualTo(250).Within(0.01f), 
                "Destinatia trebuie sa aiba 250 RON");
            Assert.Pass("Transferul EUR->RON cu stub a reusit");
        }

        // Test Stub 5: Verifica rejectia sumelor negative
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
        // TESTE FUNCȚIONALITĂȚI NOI (11 teste)
        // =====================================================================

        // Istoric tranzacții - 2 teste
        [Test, Category("pass")]
        [Description("Verifica ca istoricul de tranzactii inregistreaza corect depunerile")]
        public void TransactionHistory_RecordsDeposits_Correctly()
        {
            Account account = new Account();
            account.Deposit(100);
            account.Deposit(200);
            account.Deposit(50);
            
            var history = account.GetTransactionHistory();
            
            Assert.That(history.Count, Is.EqualTo(3), "Ar trebui sa existe 3 tranzactii");
            Assert.That(history[2].BalanceAfter, Is.EqualTo(350));
        }

        // Blocare cont - 2 teste
        [Test, Category("pass")]
        [Description("Verifica ca un cont blocat nu permite depuneri")]
        public void LockedAccount_PreventDeposit_ThrowsException()
        {
            Account account = new Account();
            account.Deposit(500);
            account.LockAccount(1);
            
            Assert.Throws<bank.AccountLockedException>(() => account.Deposit(100));
        }

        [Test, Category("pass")]
        [Description("Verifica ca deblocarea contului permite din nou operatiuni")]
        public void UnlockedAccount_AllowsOperations_Successfully()
        {
            Account account = new Account();
            account.Deposit(500);
            account.LockAccount(1);
            account.UnlockAccount();
            account.Deposit(100);
            
            Assert.That(account.Balance, Is.EqualTo(600));
            Assert.That(account.IsLocked, Is.False);
        }

        // Limită zilnică - 2 teste
        [Test, Category("pass")]
        [Description("Verifica ca limita zilnica previne transferuri prea mari")]
        public void DailyLimit_PreventExcessiveTransfers_ThrowsException()
        {
            Account source = new Account();
            source.Deposit(10000);
            source.SetDailyLimit(1000);
            Account destination = new Account();
            destination.Deposit(100);
            
            source.TransferMinFunds(destination, 600);
            
            Assert.Throws<bank.DailyLimitExceededException>(() => 
                source.TransferMinFunds(destination, 500));
        }

        [Test, Category("pass")]
        [Description("Verifica ca limita zilnica permite transferuri sub limita")]
        public void DailyLimit_AllowsTransfersUnderLimit_Successfully()
        {
            Account source = new Account();
            source.Deposit(5000);
            source.SetDailyLimit(2000);
            Account destination = new Account();
            destination.Deposit(100);
            
            source.TransferMinFunds(destination, 500);
            source.TransferMinFunds(destination, 400);
            source.TransferMinFunds(destination, 300);
            
            Assert.That(source.TotalTransferredToday, Is.EqualTo(1200));
        }

        // Dobândă - 2 teste
        [Test, Category("pass")]
        [Description("Verifica setarea ratei dobanzii")]
        public void SetInterestRate_UpdatesRate_Successfully()
        {
            Account account = new Account();
            account.Deposit(1000);
            account.SetInterestRate(0.05f);
            
            Assert.That(account.InterestRate, Is.EqualTo(0.05f));
        }

        [Test, Category("pass")]
        [Description("Verifica ca rata dobanzii invalida arunca exceptie")]
        public void SetInterestRate_InvalidRate_ThrowsException()
        {
            Account account = new Account();
            
            Assert.Throws<ArgumentException>(() => account.SetInterestRate(-0.01f));
        }

        // Statistici - 2 teste
        [Test, Category("pass")]
        [Description("Verifica calculul statisticilor contului")]
        public void GetStatistics_CalculatesCorrectly()
        {
            Account account = new Account();
            account.Deposit(500);
            account.Deposit(300);
            account.Withdraw(150);
            
            var stats = account.GetStatistics();
            
            Assert.That(stats.TotalDeposits, Is.EqualTo(800));
            Assert.That(stats.TotalWithdrawals, Is.EqualTo(150));
        }

        // Securitate - 1 test
        [Test, Category("pass")]
        [Description("Verifica ca un cont valid este considerat sigur")]
        public void IsAccountSecure_ValidAccount_ReturnsTrue()
        {
            Account account = new Account();
            account.Deposit(500);
            
            bool isSecure = account.IsAccountSecure();
            
            Assert.That(isSecure, Is.True);
        }

        // Test complex - 2 teste
        [Test, Category("pass")]
        [Description("Test complex: transferuri multiple cu verificare istoric si statistici")]
        public void ComplexScenario_MultipleTransfers_WithHistoryAndStats()
        {
            Account source = new Account();
            source.Deposit(2000);
            source.SetDailyLimit(1500);
            Account destination = new Account();
            destination.Deposit(500);
            
            source.TransferMinFunds(destination, 300);
            source.TransferMinFunds(destination, 200);
            
            Assert.That(source.Balance, Is.EqualTo(1500));
            Assert.That(destination.Balance, Is.EqualTo(1000));
        }

        [Test, Category("pass")]
        [Description("Test edge case: transfer exact la limita de 40%")]
        public void EdgeCase_ExactFourtyPercentTransfer_LargeBalance()
        {
            Account source = new Account();
            source.Deposit(10000);
            Account destination = new Account();
            destination.Deposit(1000);
            
            source.TransferMinFunds(destination, 4000);
            
            Assert.That(source.Balance, Is.EqualTo(6000));
        }

        // =====================================================================
        // ========== TESTE MOCK (5 teste) ==========
        // =====================================================================
        // Mock Object = un obiect de test care simuleaza comportamentul unei dependente externe
        // Si permite VERIFICAREA ca metodele au fost apelate corect (spre deosebire de STUB)
        
        // Test Mock 1: Verifica ca metoda GetEurToRonRate() este apelata exact o data
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

        // Test Mock 2: Verifica conversii multiple
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

        // Test Mock 3: Verifica ca nu se apeleaza GetEurToRonRate() pentru sume negative
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

        // Test Mock 4: Moq framework - verifica apelul metodei
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

        // Test Mock 5: Moq framework - verifica ca metoda NU este apelata
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
    }
}