using Discord;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Energize.Essentials
{
    public enum EmbedColorType
    {
        Good = 0,
        Warning = 1,
        Danger = 2,
        Normal = 3,
    }

    public static class Extensions
    {
        public static EmbedBuilder WithField(this EmbedBuilder builder, string title, object value, bool inline = true)
        {
            if (string.IsNullOrWhiteSpace(title)) return builder;

            if (value == null) return builder;
            string val = value.ToString();
            if (string.IsNullOrWhiteSpace(val)) return builder;

            EmbedFieldBuilder fieldBuilder = new EmbedFieldBuilder();
            fieldBuilder
                .WithIsInline(inline)
                .WithName(title)
                .WithValue(val.Length > 1024 ? $"{val.Substring(0, 1021)}..." : val);

            return builder.WithFields(fieldBuilder);
        }

        public static EmbedBuilder WithLimitedTitle(this EmbedBuilder builder, string title)
            => string.IsNullOrWhiteSpace(title)
                ? builder
                : builder.WithTitle(title.Length > 256 ? $"{title.Substring(0, 253)}..." : title);

        public static EmbedBuilder WithLimitedDescription(this EmbedBuilder builder, string description)
            => string.IsNullOrWhiteSpace(description)
                ? builder
                : builder.WithDescription(description.Length > 2048
                    ? $"{description.Substring(0, 2045)}..."
                    : description);

        public static EmbedBuilder WithAuthorNickname(this EmbedBuilder builder, IMessage msg)
            => builder.WithAuthorNickname(msg.Author);

        public static EmbedBuilder WithAuthorNickname(this EmbedBuilder builder, IUser user)
        {
            if (user is IGuildUser author)
            {
                string nick = author.Nickname != null ? $"{author.Nickname} ({author})" : author.ToString();
                string url = author.GetAvatarUrl(ImageFormat.Auto, 32);
                builder.WithAuthor(nick, url);
            }
            else
            {
                builder.WithAuthor(user);
            }

            return builder;
        }

        public static EmbedBuilder WithColorType(this EmbedBuilder builder, EmbedColorType colorType)
        {
            switch(colorType)
            {
                case EmbedColorType.Good:
                    return builder.WithColor(MessageSender.SColorGood);
                case EmbedColorType.Warning:
                    return builder.WithColor(MessageSender.SColorWarning);
                case EmbedColorType.Danger:
                    return builder.WithColor(MessageSender.SColorDanger);
                case EmbedColorType.Normal:
                    return builder.WithColor(MessageSender.SColorNormal);
                default:
                    return builder.WithColor(MessageSender.SColorNormal);
            }
        }

        private static readonly string[] ValidExtensions = new string[] { "mp3", "mp4", "ogg", "wav", "webm", "mov" };
        private static readonly Regex UrlExtensionRegex = new Regex(@"https?:\/\/[^\s\/]+\/[^\s\.]+\.([A-Za-z0-9]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static bool IsPlayableUrl(this string url)
        {
            if (HttpClient.IsUrl(url))
            {
                if (!url.StartsWith("http")) return false;

                Match match = UrlExtensionRegex.Match(url);
                if (!match.Success) return false;

                string extension = match.Groups[1].Value;
                return ValidExtensions.Any(ext => ext.Equals(extension));
            }

            FileInfo fileInfo = new FileInfo(url);
            if (string.IsNullOrWhiteSpace(fileInfo.Extension) || fileInfo.Extension.Length < 2) return false; // 2 = ".|xxxx" 
            return ValidExtensions.Any(ext => ext.Equals(fileInfo.Extension.Substring(1)));
        }

        public static bool IsPlayableAttachment(this IAttachment attachment)
            => attachment.Filename.IsPlayableUrl();

        public static bool FuzzyMatch(this string stringToSearch, string pattern, out int outScore)
        {
            // Score consts
            const int adjacencyBonus = 5; // bonus for adjacent matches
            const int separatorBonus = 10; // bonus if match occurs after a separator
            const int camelBonus = 10;     // bonus if match is uppercase and prev is lower

            const int leadingLetterPenalty = -3;    // penalty applied for every letter in stringToSearch before the first match
            const int maxLeadingLetterPenalty = -9; // maximum penalty for leading letters
            const int unmatchedLetterPenalty = -1;  // penalty for every letter that doesn't matter

            // Loop variables
            int score = 0;
            int patternIdx = 0;
            int patternLength = pattern.Length;
            int strIdx = 0;
            int strLength = stringToSearch.Length;
            bool prevMatched = false;
            bool prevLower = false;
            bool prevSeparator = true; // true if first letter match gets separator bonus

            // Use "best" matched letter if multiple string letters match the pattern
            char? bestLetter = null;
            char? bestLower = null;
            int? bestLetterIdx = null;
            int bestLetterScore = 0;

            List<int> matchedIndices = new List<int>();

            // Loop over strings
            while (strIdx != strLength)
            {
                char? patternChar = patternIdx != patternLength ? pattern[patternIdx] as char? : null;
                char strChar = stringToSearch[strIdx];

                char? patternLower = patternChar != null ? char.ToLower((char)patternChar) as char? : null;
                char strLower = char.ToLower(strChar);
                char strUpper = char.ToUpper(strChar);

                bool nextMatch = patternChar != null && patternLower == strLower;
                bool rematch = bestLetter != null && bestLower == strLower;

                bool advanced = nextMatch && bestLetter != null;
                bool patternRepeat = bestLetter != null && patternChar != null && bestLower == patternLower;
                if (advanced || patternRepeat)
                {
                    score += bestLetterScore;
                    matchedIndices.Add((int)bestLetterIdx);
                    bestLetter = null;
                    bestLower = null;
                    bestLetterIdx = null;
                    bestLetterScore = 0;
                }

                if (nextMatch || rematch)
                {
                    int newScore = 0;

                    // Apply penalty for each letter before the first pattern match
                    // Note: Math.Max because penalties are negative values. So max is smallest penalty.
                    if (patternIdx == 0)
                    {
                        int penalty = Math.Max(strIdx * leadingLetterPenalty, maxLeadingLetterPenalty);
                        score += penalty;
                    }

                    // Apply bonus for consecutive bonuses
                    if (prevMatched)
                        newScore += adjacencyBonus;

                    // Apply bonus for matches after a separator
                    if (prevSeparator)
                        newScore += separatorBonus;

                    // Apply bonus across camel case boundaries. Includes "clever" isLetter check.
                    if (prevLower && strChar == strUpper && strLower != strUpper)
                        newScore += camelBonus;

                    // Update pattern index IF the next pattern letter was matched
                    if (nextMatch)
                        ++patternIdx;

                    // Update best letter in stringToSearch which may be for a "next" letter or a "rematch"
                    if (newScore >= bestLetterScore)
                    {
                        // Apply penalty for now skipped letter
                        if (bestLetter != null)
                            score += unmatchedLetterPenalty;

                        bestLetter = strChar;
                        bestLower = char.ToLower((char)bestLetter);
                        bestLetterIdx = strIdx;
                        bestLetterScore = newScore;
                    }

                    prevMatched = true;
                }
                else
                {
                    score += unmatchedLetterPenalty;
                    prevMatched = false;
                }

                // Includes "clever" isLetter check.
                prevLower = strChar == strLower && strLower != strUpper;
                prevSeparator = strChar == '_' || strChar == ' ';

                ++strIdx;
            }

            // Apply score for last match
            if (bestLetter != null)
            {
                score += bestLetterScore;
                matchedIndices.Add((int)bestLetterIdx);
            }

            outScore = score;
            return patternIdx == patternLength;
        }
    }
}
