using System;
using Victoria.Entities;
using Victoria.Queue;

namespace Energize.Essentials.TrackTypes
{
    public interface ITrack : IQueueObject
    {
        LavaTrack InnerTrack { get; }

        bool IsSeekable { get; }

        string Author { get; }

        bool IsStream { get; }

        TimeSpan Position { get; }

        TimeSpan Length { get; }

        string Title { get; }

        Uri Uri { get; }

        string Provider { get; }

        void ResetPosition();
    }
}
