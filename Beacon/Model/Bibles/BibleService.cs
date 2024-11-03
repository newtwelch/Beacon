using AsyncAwaitBestPractices;
using SQLite;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Windows.Controls.Primitives;

namespace Beacon.Model.Bibles
{
    public class BibleService : IBibleService
    {
        private SQLiteAsyncConnection dbConnection;
        private HttpClient httpClient;

        public BibleService(ICustomHttpFactory factory)
        {
            dbConnection = new SQLiteAsyncConnection(Constants.BibleDbPath, Constants.Flags);
            httpClient = factory.GetClient();
            CreateTable().SafeFireAndForget();
        }
        public async Task CreateTable() => await dbConnection.CreateTableAsync<Bible>();

        //[GET]=========================================
        public async Task<List<Book>> GetBooksAsync(string translation, bool useEnglish = false)
            => useEnglish ? await dbConnection.QueryAsync<Book>($"SELECT * FROM kjvBooks").ConfigureAwait(false) :
                            await dbConnection.QueryAsync<Book>($"SELECT * FROM {translation}Books");
        public async Task<List<Chapter>> GetChaptersAsync(string translation, int book)
            => await dbConnection.QueryAsync<Chapter>($"SELECT DISTINCT Chapter FROM {translation} WHERE Book = {book}");
        public async Task<List<BeaconVerse>> GetVersesAsync(string translation, int book, int chapter)
            => await dbConnection.QueryAsync<BeaconVerse>($"SELECT * FROM {translation} WHERE Book = {book} AND Chapter = {chapter}");
        public async Task<List<Bible>> GetBiblesAsync() => await dbConnection.QueryAsync<Bible>("SELECT * FROM Bibles");

        //[SEARCH]=========================================
        public async Task<List<Book>> SearchBooksAsync(string translation, string searchTerm, bool useEnglish = false)
        {
            var books = new List<Book>();

            if (useEnglish)
                books = await dbConnection.QueryAsync<Book>($"SELECT * FROM kjvaBooks WHERE Name LIKE '%{searchTerm}%'");
            else
                books = await dbConnection.QueryAsync<Book>($"SELECT * FROM {translation}Books WHERE Name LIKE '%{searchTerm}%'");


            // Give HIGHLIGHT to the BOOK QUERY
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                foreach (var book in books)
                {
                    int startIndex = book.Name.ToLower().IndexOf(searchTerm.ToLower());
                    int endIndex = startIndex + searchTerm.Length;
                    string wrap = book.Name.Substring(startIndex, endIndex - startIndex);
                    wrap = book.Name.Replace(wrap, "<span class=\"text-orange group-hover:text-white_light\">" + wrap + "</span>");
                    book.Name = wrap;
                }
            }

            return books;
        }

        public async Task<List<BeaconVerse>> SearchVerseFromChapterAsync(string translation, int Book, int Chapter, string searchTerm)
        {
            var verses = new List<BeaconVerse>();

            searchTerm = searchTerm.Trim().Replace("'", " ").Replace(" ", "* ");
            verses = await dbConnection.QueryAsync<BeaconVerse>($"SELECT Book, BookName, Chapter, Verse, highlight({translation}, 4, '<span class=\"text-orange group-hover:text-white_light\">', '</span>') as Text FROM {translation} WHERE Text MATCH '\"{searchTerm}\"*' AND Book = {Book} AND Chapter = {Chapter}");

            return verses;
        }

        public async Task<List<BeaconVerse>> SearchVerseGloballyAsync(string translation, string searchTerm)
        {
            var verses = new List<BeaconVerse>();

            searchTerm = searchTerm.Trim().Replace("'", " ").Replace(" ", "* ");
            verses = await dbConnection.QueryAsync<BeaconVerse>($"SELECT Book, BookName, Chapter, Verse, highlight({translation}, 4, '<span class=\"text-orange group-hover:text-white_light\">', '</span>') as Text FROM {translation} WHERE Text MATCH '\"{searchTerm}\"*' LIMIT 50");

            return verses;
        }

        //[API]=========================================
        public async Task DownloadBible(string translation)
        {
            //CHECK IF EXISTS
            bool languageExists = await dbConnection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM Bibles WHERE Abbreviation = '{translation}'").ConfigureAwait(false) > 0;
            if (languageExists)
                return;

            //GET BIBLE FROM API
            var bible = await GetAPIBibleAsync(translation);

            //CREATE TABLE
            await dbConnection.ExecuteAsync($"CREATE VIRTUAL TABLE IF NOT EXISTS {translation} USING Fts5(Book, BookName, Chapter, Verse, Text)");
            await dbConnection.ExecuteAsync($"CREATE TABLE IF NOT EXISTS {translation}Books (Id INTEGER NOT NULL, Name TEXT NOT NULL)");
            await dbConnection.InsertAsync(bible);

            //CONVERT AND INSERT TO TABLE
            await ConvertAndSaveToDatabase(bible);
            await dbConnection.ExecuteAsync("VACUUM");
        }

        public async Task<Bible> GetAPIBibleAsync(string translation)
        {
            HttpResponseMessage responseMessage = await httpClient.GetAsync($"https://api.getbible.net/v2/{translation}.json");

            var bible = new Bible();
            if (responseMessage.IsSuccessStatusCode)
                bible = await responseMessage.Content.ReadFromJsonAsync<Bible>();

            return bible!;
        }
        private async Task ConvertAndSaveToDatabase(Bible bible)
        {
            await dbConnection.RunInTransactionAsync(nonAsync =>
            {
                foreach (var book in bible.Books)
                {
                    nonAsync.Execute($"INSERT INTO {bible.Abbreviation}Books (Id, Name) VALUES (?, ?)", book.Id, book.Name);

                    foreach (var chapter in book.Chapters)
                    {
                        foreach(var verse in chapter.Verses)
                        {
                            nonAsync.Execute($"INSERT INTO {bible.Abbreviation} (Book, BookName, Chapter, Verse, Text) VALUES (?, ?, ?, ?, ?)",
                                                                                      book.Id, book.Name, chapter.Id, verse.Id, verse.Text);
                        }
                    }
                }
            });
        }
    }

    public interface IBibleService
    {
        public Task<Bible> GetAPIBibleAsync(string translation);
        public Task<List<Bible>> GetBiblesAsync();
        public Task<List<Book>> GetBooksAsync(string translation, bool useEnglish = false);
        public Task<List<Chapter>> GetChaptersAsync(string translation, int book);
        public Task<List<BeaconVerse>> GetVersesAsync(string translation, int book, int chapter);
        public Task<List<Book>> SearchBooksAsync(string translation, string searchTerm, bool useEnglish = false);
        public Task DownloadBible(string translation);
        public Task<List<BeaconVerse>> SearchVerseFromChapterAsync(string translation, int Book, int Chapter, string searchTerm);
        public Task<List<BeaconVerse>> SearchVerseGloballyAsync(string translation, string searchTerm);
    }
}
