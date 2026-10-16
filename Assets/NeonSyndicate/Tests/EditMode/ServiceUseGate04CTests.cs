using System;
using NUnit.Framework;

namespace NeonSyndicate.Tests
{
    public sealed class ServiceUseGate04CTests
    {
        [Test]
        public void LosingAccessAfterFocusDoesNotChargeOrProduceOutput()
        {
            bool available = true;
            int cash = 100, heals = 0;
            ServiceUseGate04C gate = new ServiceUseGate04C(() => available, () => { cash -= 100; heals++; });
            available = false;
            Assert.That(gate.TryUse(), Is.False);
            Assert.That(cash, Is.EqualTo(100));
            Assert.That(heals, Is.Zero);
            available = true;
            Assert.That(gate.TryUse(), Is.True);
            Assert.That(cash, Is.Zero);
            Assert.That(heals, Is.EqualTo(1));
        }

        [Test]
        public void ReentrantCallbackCannotSpendTwice()
        {
            int payments = 0;
            ServiceUseGate04C gate = null;
            gate = new ServiceUseGate04C(() => true, () =>
            {
                payments++;
                Assert.That(gate.TryUse(), Is.False);
            });
            Assert.That(gate.TryUse(), Is.True);
            Assert.That(payments, Is.EqualTo(1));
        }

        [Test]
        public void FailedCallbackDoesNotPermanentlyLockOriginalService()
        {
            bool fail = true;
            int uses = 0;
            ServiceUseGate04C gate = new ServiceUseGate04C(() => true, () =>
            {
                if (fail) throw new InvalidOperationException("source service unavailable");
                uses++;
            });
            Assert.Throws<InvalidOperationException>(() => gate.TryUse());
            fail = false;
            Assert.That(gate.TryUse(), Is.True);
            Assert.That(uses, Is.EqualTo(1));
        }

        [Test]
        public void MissingServiceOrAvailabilityCannotInvokeTransaction()
        {
            int uses = 0;
            Assert.That(new ServiceUseGate04C(null, () => uses++).TryUse(), Is.False);
            Assert.That(new ServiceUseGate04C(() => true, null).TryUse(), Is.False);
            Assert.That(uses, Is.Zero);
        }
    }
}
