namespace SlimeFighter
{
    public interface HittableObject
    {
        int XPos { get; }
        int YPos { get; }
        bool Inactive { get; }

        public void TakeDamage(int damage);
    }
}
