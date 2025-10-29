using System.Collections.Generic;

namespace UI.Lobby.GameSetup
{
    public class GameOptionField<T>
    {
        public T Original { get; private set; }
        public T Current { get; set; }

        public GameOptionField(T initialValue)
        {
            Original = initialValue;
            Current = initialValue;
        }

        public bool IsDirty => !EqualityComparer<T>.Default.Equals(Original, Current);

        public void Apply()
        {
            Original = Current;
        }

        public void Reset()
        {
            Current = Original;
        }

        public override string ToString()
        {
            return $"Origin: {Original} Current: {Current}";
        }
    }
}