using System;
using Victoria.Entities;

namespace Energize.Essentials.TrackTypes
{
    /// <summary>
    /// Simple implementation of ITrack that redirects all fields to the required innerTrack
    /// </summary>
    public class DependentTrack : ITrack
    {
        public LavaTrack InnerTrack { get; }
        
        public string Id { get; }

        public bool IsSeekable => InnerTrack.IsSeekable;

        public string Author => InnerTrack.Author;

        public bool IsStream => InnerTrack.IsStream;

        public TimeSpan Position => InnerTrack.Position;

        public TimeSpan Length => InnerTrack.Length;

        public string Title => InnerTrack.Title;

        public Uri Uri => InnerTrack.Uri;

        public string Provider => InnerTrack.Provider;

        protected DependentTrack()
        {
        }

        internal DependentTrack(LavaTrack innerTrack, string id = null)
        {
            InnerTrack = innerTrack;
            Id = id ?? innerTrack.Id;
        }

        public void ResetPosition() => InnerTrack.ResetPosition();
    }
}