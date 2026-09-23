namespace OutpostZero.Sensory
{
    /// <summary>
    /// A footstep's base reach before surface, puddle and limp change it.
    /// The noise book's row wins; without a book the leader's own Inspector value stands.
    /// </summary>
    public static class StepNoise
    {
        public static float Base(float field, string id)
        {
            if (NoiseTable.Has(id)) return NoiseTable.Radius(id);
            return field > 0f ? field : 0f;
        }
    }
}
