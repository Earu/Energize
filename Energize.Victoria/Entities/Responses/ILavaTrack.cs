using System;
using Victoria.Queue;

namespace Victoria.Entities
{
    /// <summary>
    /// Interface for LavaTrack, has all of the public required properties for playing a track 
    /// </summary>
    public interface ILavaTrack : IQueueObject
    {
        string Hash { get; set; }

        bool IsSeekable { get; }

        string Author { get; }

        bool IsStream { get; }

        TimeSpan Position { get; set; }

        TimeSpan Length { get; }

        bool HasLength { get; }

        string Title { get; }

        Uri Uri { get; }

        string Provider { get; }
        
        void ResetPosition();
    }
}