using System;
using Victoria.Entities;

namespace Energize.Essentials.TrackTypes
{
    internal class EasyLavaTrack : LavaTrack
    {
        private readonly Action _resetPositionCallback;
        
        public override string Id { get; set; }
        
        public override bool IsSeekable { get; set; }
        
        public override string Author { get; set; }

        public override bool IsStream { get; set; }
        
        public override TimeSpan Position { get; set; }
        
        public override TimeSpan Length { get; }
        
        public override string Title { get; set; }
        
        public override Uri Uri { get; set; }

        public override string Provider { get; }

        public EasyLavaTrack(
            string id,
            bool isSeekable,
            string author,
            bool isStream,
            TimeSpan position, 
            TimeSpan length,
            string title,
            Uri uri,
            string provider,
            Action resetPositionCallback)
        {
            Id = id;
            IsSeekable = isSeekable;
            Author = author;
            IsStream = isStream;
            Position = position;
            Length = length;
            Title = title;
            Uri = uri;
            Provider = provider;
            _resetPositionCallback = resetPositionCallback;
        }

        public override void ResetPosition() => _resetPositionCallback();
    }
}