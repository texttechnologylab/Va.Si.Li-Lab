
namespace VaSiLi.Logging
{
    public interface IContinousLogCollector<T> : ILogCollector<T> 
    {
        public void PollData(float timestamp);
        public float PollingRate { get; }
    }
}