using System;
using Victoria.Queue;

namespace Victoria.Entities
{
    public interface ILavaTrack : IQueueObject
    {
        string Hash { get; set; }

        bool IsSeekable { get; }

        string Author { get; }

        bool IsStream { get; }

        TimeSpan Position { get; set; }

        TimeSpan Length { get; }

        string Title { get; }

        Uri Uri { get; }

        string Provider { get; }
        
        void ResetPosition();
    }
}