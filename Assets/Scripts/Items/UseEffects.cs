namespace OutpostZero.Items
{
    public static class UseEffects
    {
        public const UseEffect Medkit = UseEffect.StopBleeding | UseEffect.CureInfection;

        public static bool Treats(UseEffect effects, UseEffect wanted)
        {
            return wanted != UseEffect.None && (effects & wanted) == wanted;
        }
    }
}
