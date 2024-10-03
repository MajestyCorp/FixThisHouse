namespace FixThisHouse
{
    /// <summary>
    /// Interface for all managers that should be initialized when game starts
    /// </summary>
    public interface IInitializer
    {
        void InitializeSelf();
        void InitializeAfter();
    }
}
