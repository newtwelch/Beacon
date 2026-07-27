using Backend.Core.Models.Bible;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Backend.Core.Services
{
    public class BookAliasService
    {
        public static readonly Dictionary<int, string[]> Aliases = new()
    {
        { 1,  new[] { "genesis", "gen" } },
        { 2,  new[] { "exodus", "exo", "exod" } },
        { 3,  new[] { "leviticus", "lev" } },
        { 4,  new[] { "numbers", "num", "nm" } },
        { 5,  new[] { "deuteronomy", "deut", "dt" } },
        { 6,  new[] { "joshua", "josh" } },
        { 7,  new[] { "judges", "judg", "jdg" } },
        { 8,  new[] { "ruth" } },
        { 9,  new[] { "1samuel", "1 samuel", "firstsamuel", "1sam", "1 sam" } },
        { 10, new[] { "2samuel", "2 samuel", "secondsamuel", "2sam", "2 sam" } },
        { 11, new[] { "1kings", "1 kings", "firstkings", "1kgs", "1 kgs" } },
        { 12, new[] { "2kings", "2 kings", "secondkings", "2kgs", "2 kgs" } },
        { 13, new[] { "1chronicles", "1 chronicles", "firstchronicles", "1chr", "1 chr" } },
        { 14, new[] { "2chronicles", "2 chronicles", "secondchronicles", "2chr", "2 chr" } },
        { 15, new[] { "ezra" } },
        { 16, new[] { "nehemiah", "neh" } },
        { 17, new[] { "esther", "esth" } },
        { 18, new[] { "job" } },
        { 19, new[] { "psalm", "psalms", "ps", "psa" } },
        { 20, new[] { "proverbs", "prov", "pr" } },
        { 21, new[] { "ecclesiastes", "eccl", "ecc" } },
        { 22, new[] { "song", "songofsolomon", "song of solomon", "canticles", "canticle", "songofsongs" } },
        { 23, new[] { "isaiah", "isa" } },
        { 24, new[] { "jeremiah", "jer" } },
        { 25, new[] { "lamentations", "lam" } },
        { 26, new[] { "ezekiel", "ezek" } },
        { 27, new[] { "daniel", "dan" } },
        { 28, new[] { "hosea", "hos" } },
        { 29, new[] { "joel" } },
        { 30, new[] { "amos" } },
        { 31, new[] { "obadiah", "obad" } },
        { 32, new[] { "jonah", "jon" } },
        { 33, new[] { "micah", "mic" } },
        { 34, new[] { "nahum", "nah" } },
        { 35, new[] { "habakkuk", "hab" } },
        { 36, new[] { "zephaniah", "zeph" } },
        { 37, new[] { "haggai", "hag" } },
        { 38, new[] { "zechariah", "zech" } },
        { 39, new[] { "malachi", "mal" } },

        { 40, new[] { "matthew", "matt", "mt" } },
        { 41, new[] { "mark", "mk", "mrk" } },
        { 42, new[] { "luke", "lk", "luk" } },
        { 43, new[] { "john", "jn", "jhn" } },
        { 44, new[] { "acts", "act" } },
        { 45, new[] { "romans", "rom" } },
        { 46, new[] { "1corinthians", "1 corinthians", "firstcorinthians", "1cor", "1 cor" } },
        { 47, new[] { "2corinthians", "2 corinthians", "secondcorinthians", "2cor", "2 cor" } },
        { 48, new[] { "galatians", "gal" } },
        { 49, new[] { "ephesians", "eph" } },
        { 50, new[] { "philippians", "phil", "php" } },
        { 51, new[] { "colossians", "col" } },
        { 52, new[] { "1thessalonians", "1 thessalonians", "firstthessalonians", "1thess", "1 thes" } },
        { 53, new[] { "2thessalonians", "2 thessalonians", "secondthessalonians", "2thess", "2 thes" } },
        { 54, new[] { "1timothy", "1 timothy", "firsttimothy", "1tim", "1 tim" } },
        { 55, new[] { "2timothy", "2 timothy", "secondtimothy", "2tim", "2 tim" } },
        { 56, new[] { "titus", "tit" } },
        { 57, new[] { "philemon", "phlm", "phm" } },
        { 58, new[] { "hebrews", "heb" } },
        { 59, new[] { "james", "jas", "jm" } },
        { 60, new[] { "1peter", "1 peter", "firstpeter", "1pet", "1 pet" } },
        { 61, new[] { "2peter", "2 peter", "secondpeter", "2pet", "2 pet" } },
        { 62, new[] { "1john", "1 john", "firstjohn", "1jn", "1 jn" } },
        { 63, new[] { "2john", "2 john", "secondjohn", "2jn", "2 jn" } },
        { 64, new[] { "3john", "3 john", "thirdjohn", "3jn", "3 jn" } },
        { 65, new[] { "jude" } },
        { 66, new[] { "revelation", "revelations", "rev", "apocalypse" } },
    };

        public static string Normalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";

            text = text.ToLowerInvariant();

            text = text
                .Replace("iii", "3")
                .Replace("ii", "2")
                .Replace("i", "1");

            text = Regex.Replace(text, @"[^a-z0-9]", "");

            return text;
        }

        public static bool Matches(Book book, string search)
        {
            if (string.IsNullOrWhiteSpace(search))
                return true;

            search = Normalize(search);

            // Match translated display name
            if (Normalize(book.Name).Contains(search))
                return true;

            // Match English aliases
            if (Aliases.TryGetValue(book.Id, out var aliases))
            {
                foreach (var alias in aliases)
                {
                    if (Normalize(alias).Contains(search))
                        return true;
                }
            }

            return false;
        }
    }
}
