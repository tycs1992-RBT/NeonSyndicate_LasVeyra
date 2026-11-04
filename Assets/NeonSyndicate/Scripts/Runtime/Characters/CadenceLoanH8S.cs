namespace NeonSyndicate
{
    /// <summary>
    /// Engine-free loan of a shared on/off flag (the original animator's AlwaysAnimate cadence flag). Take records the value
    /// it found and switches the flag on; Release gives the recorded value back. Several borrowers stay consistent only when
    /// they release in the reverse order of taking, so the Dante living cast review ends any quiet listening loop before it
    /// takes the flag, and its release ends the loop before giving the flag back. The tests run every start and end order
    /// of the two borrowers through this model.
    /// </summary>
    public struct CadenceLoanH8S
    {
        private bool _prior;

        /// <summary>True while this borrower holds the flag.</summary>
        public bool Owned { get; private set; }

        /// <summary>Takes the flag: remembers current and returns the value to write (on). A second Take keeps the first record.</summary>
        public bool Take(bool current)
        {
            if (Owned) return true;
            _prior = current;
            Owned = true;
            return true;
        }

        /// <summary>Gives the flag back: returns the value to write (the recorded one), or current when not held.</summary>
        public bool Release(bool current)
        {
            if (!Owned) return current;
            Owned = false;
            return _prior;
        }
    }
}
