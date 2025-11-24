using System;

namespace bank
{
    // Interfata simpla pentru conversie valutara
    public interface ICurrencyConverter
    {
        // Obtine cursul de schimb EUR -> RON (cati lei pentru 1 EUR)
        float GetEurToRonRate();
    }

    // Stub simplu pentru teste - curs fix
    public class CurrencyConverterStub : ICurrencyConverter
    {
        private readonly float rate;

        public CurrencyConverterStub(float eurToRonRate)
        {
            rate = eurToRonRate;
        }

        public float GetEurToRonRate()
        {
            return rate;
        }
    }

    // =====================================================================
    // MOCK - pentru verificarea apelurilor metodelor
    // =====================================================================
    // Mock Object = obiect de test care:
    //  1. Simuleaza comportamentul unei dependente (ca STUB)
    //  2. TINE EVIDENTA apelurilor metodelor (numar, parametri, ordine)
    //  3. Permite VERIFICARI despre cum a fost folosit (spre deosebire de STUB)
    // =====================================================================
    public class CurrencyConverterMock : ICurrencyConverter
    {
        private float rate;
        private int getRateCallCount = 0;
        private bool wasCalled = false;
        private List<DateTime> callTimestamps = new List<DateTime>();

        public CurrencyConverterMock(float eurToRonRate)
        {
            rate = eurToRonRate;
        }

        public float GetEurToRonRate()
        {
            // Inregistreaza fiecare apel
            getRateCallCount++;
            wasCalled = true;
            callTimestamps.Add(DateTime.Now);
            
            return rate;
        }

        // Proprietati pentru verificari in teste
        public int GetRateCallCount => getRateCallCount;
        public bool WasCalled => wasCalled;
        public List<DateTime> CallTimestamps => new List<DateTime>(callTimestamps);

        // Metode helper pentru teste
        public void Reset()
        {
            getRateCallCount = 0;
            wasCalled = false;
            callTimestamps.Clear();
        }

        public void SetRate(float newRate)
        {
            rate = newRate;
        }

        public int GetCallCountBetween(DateTime start, DateTime end)
        {
            return callTimestamps.Count(t => t >= start && t <= end);
        }
    }

    // Implementare reala - cursul de la BNR
    public class BnrCurrencyConverter : ICurrencyConverter
    {
        public float GetEurToRonRate()
        {
            // TODO: fetch-ezi cursul de la https://www.bnr.ro/nbrfxrates.xml
            return 4.97f; // curs aproximativ pentru moment
        }
    }
}
