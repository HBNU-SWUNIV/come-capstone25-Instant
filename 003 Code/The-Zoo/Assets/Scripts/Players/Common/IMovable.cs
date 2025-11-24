namespace Players.Common
{
    public interface IMovable
    {
        bool CanMove { get; set; }
        bool CanJump { get; set; }
        bool SpinHold { get; set; }
    }
}