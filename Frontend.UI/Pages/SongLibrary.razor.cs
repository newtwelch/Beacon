using Backend.Core.Models.Songs;
using Backend.Core.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Frontend.UI.Pages
{
    public partial class SongLibrary : IDisposable
    {
        [Parameter] public Func<bool> ShouldReRender { get; set; }

        private List<Song> songs = new();
        private Song? selectedSong;
        private Song? backUpSong;
        private Song? pendingDeleteSong;
        private List<Song>? languagesOfSelectedSong = new();
        private List<Lyric>? lyrics = new();
        private string searchText = "";
        private bool isQueueMode = false;
        private bool isEditMode = false;
        private bool showConfirmDelete = false;

        protected override async Task OnInitializedAsync()
        {
            SongService.OnSongCollectionChanged += HandleSongCollectionChanged;
            await LoadSongsAsync();
        }
        private void HandleSongCollectionChanged() =>
        _ = InvokeAsync(async () =>
        {
            await LoadSongsAsync();

            // The open song may have just been deleted along with its collection.
            // (Assumes `songs` is the full list. If LoadSongsAsync filters by searchText,
            // check against the full list instead.)
            if (selectedSong != null && !songs.Any(s => s.Id == selectedSong.Id))
            {
                selectedSong = null;
                backUpSong = null;
                pendingDeleteSong = null;
                languagesOfSelectedSong = new();
                lyrics = new();
                isEditMode = false;
                showConfirmDelete = false;
            }

            StateHasChanged();
        });

        public void Dispose()
        {
            SongService.OnSongCollectionChanged -= HandleSongCollectionChanged;
        }
        private void EditModeToggle() => isEditMode = !isEditMode;
        private async Task DiscardEditClick()
        {
            if (backUpSong is null || selectedSong is null) return;

            var index = songs.FindIndex(s => s.Id == selectedSong.Id);
            selectedSong = backUpSong.Clone();
            UpdateLyrics();
            EditModeToggle();
            await InvokeAsync(StateHasChanged);

            await Task.Delay(1); // Allow time for UI to update
            if (index >= 0)
            {
                await JSRuntime.InvokeVoidAsync("ScrollWithHeight", "SongList", index);
            }

        }

        private async Task SaveEditClick()
        {
            if (selectedSong is null) return;
            var result = await SongService.UpdateAsync(selectedSong).ConfigureAwait(false);
            if (!result.Success)
            {
                Logger.LogError("Failed to save song edits: {Error}", result.ErrorMessage);
                return;
            }
            backUpSong = selectedSong.Clone();

            var index = songs.FindIndex(s => s.Id == selectedSong.Id);
            if (index != -1)
            {
                songs[index] = selectedSong.Clone();
                songs[index].LyricText = ""; // Clear lyric text in list view
                songs[index].Key = "";
                songs[index].Tags = "";
            }

            var langResult = await SongService.GetLanguagesAsync(selectedSong.Number).ConfigureAwait(false);
            languagesOfSelectedSong = langResult.Success ? langResult.Data : new List<Song>();

            EditModeToggle();
            await InvokeAsync(StateHasChanged);

            if (index >= 0)
            {
                await JSRuntime.InvokeVoidAsync("ScrollWithHeight", "SongList", index);
            }

        }

        private async Task LoadSongsAsync()
        {
            var result = isQueueMode
                ? await SongService.GetQueueAsync().ConfigureAwait(false)
                : await SongService.GetAllAsync().ConfigureAwait(false);

            if (result.Data is null) return;
            songs = result.Success ? result.Data : new List<Song>();
            await InvokeAsync(StateHasChanged);
        }

        private async Task OnSearchChanged(string value)
        {
            searchText = value;
            var result = await SearchSongs(value).ConfigureAwait(false);

            if (result.Data is null) return;
            songs = result.Success ? result.Data : new List<Song>();
        }

        private async Task<Result<List<Song>>> SearchSongs(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return isQueueMode
                    ? await SongService.GetQueueAsync()
                    : await SongService.GetAllAsync();

            return query switch
            {
                var q when q.StartsWith(".") => await SongService.QueryLyricAsync(q[1..]),
                var q when q.StartsWith("*") => await SongService.QueryAuthorAsync(q[1..]),
                var q when q.StartsWith("#") => await SongService.QueryTagAsync(q[1..]),
                _ => await SongService.QueryTitleAsync(query)
            };
        }

        private async Task SelectedSongChanged(Song song)
        {
            if (song is null) return;

            var songResult = await SongService.GetAsync(song.Id).ConfigureAwait(false);
            if (!songResult.Success)
            {
                Logger.LogWarning("Failed to load song {Id}: {Error}", song.Id, songResult.ErrorMessage);
                return;
            }
            selectedSong = songResult.Data;

            if (selectedSong is null) return;

            var langResult = await SongService.GetLanguagesAsync(selectedSong.Number).ConfigureAwait(false);
            languagesOfSelectedSong = langResult.Success ? langResult.Data : new List<Song>();

            UpdateLyrics();
            backUpSong = selectedSong.Clone();
        }

        private void UpdateLyrics() => lyrics = selectedSong?.Lyrics() ?? new List<Lyric>();

        private void SelectedLyricChanged(Lyric lyric)
        {
            if (selectedSong is not null)
                ProjectionService.ChangeLyric(selectedSong.Title, lyric.Line, lyric.Text.RemoveHighlight());
        }

        private async Task QueueModeToggle()
        {
            isQueueMode = !isQueueMode;
            await LoadSongsAsync();
            searchText = "";
        }

        private async Task AddToQueue(Song song)
        {
            var result = await SongService.AddToQueueAsync(song.Id).ConfigureAwait(false);
            if (!result.Success)
            {
                Logger.LogError("Failed to add song to queue: {Error}", result.ErrorMessage);
                return;
            }

            // Update local state
            song.InQueue = true;
            song.QueueOrder = (await SongService.GetHighestQueueOrderAsync()).Data;

            // If in queue mode, refresh the list
            if (isQueueMode)
                await LoadSongsAsync();
        }

        private async Task RemoveFromQueue(Song song)
        {
            var result = await SongService.RemoveFromQueueAsync(song.Id).ConfigureAwait(false);
            if (!result.Success)
            {
                Logger.LogError("Failed to remove song from queue: {Error}", result.ErrorMessage);
                return;
            }

            // Update local state
            song.InQueue = false;
            song.QueueOrder = 0;

            // If in queue mode, refresh the list
            if (isQueueMode)
                await LoadSongsAsync();
        }

        private async Task MoveUpInQueue(Song song)
        {
            if (!isQueueMode || songs is null) return;

            var index = songs.FindIndex(s => s.Id == song.Id);
            if (index <= 0) return; // Can't move up the first item

            var songAbove = songs[index - 1];

            // Swap queue orders
            var updates = new List<(int SongId, int NewOrder)>
        {
            (song.Id, songAbove.QueueOrder),
            (songAbove.Id, song.QueueOrder)
        };

            var result = await SongService.UpdateQueueOrderAsync(updates).ConfigureAwait(false);
            if (!result.Success)
            {
                Logger.LogError("Failed to move song up in queue: {Error}", result.ErrorMessage);
                return;
            }

            // Refresh the queue
            await LoadSongsAsync();
        }

        private async Task MoveDownInQueue(Song song)
        {
            if (!isQueueMode || songs is null) return;

            var index = songs.FindIndex(s => s.Id == song.Id);
            if (index >= songs.Count - 1) return; // Can't move down the last item

            var songBelow = songs[index + 1];

            // Swap queue orders
            var updates = new List<(int SongId, int NewOrder)>
        {
            (song.Id, songBelow.QueueOrder),
            (songBelow.Id, song.QueueOrder)
        };

            var result = await SongService.UpdateQueueOrderAsync(updates).ConfigureAwait(false);
            if (!result.Success)
            {
                Logger.LogError("Failed to move song down in queue: {Error}", result.ErrorMessage);
                return;
            }

            // Refresh the queue
            await LoadSongsAsync();
        }

        private async Task ClearQueue()
        {
            if (!isQueueMode) return;

            var result = await SongService.ClearQueueAsync().ConfigureAwait(false);
            if (!result.Success)
            {
                Logger.LogError("Failed to clear queue: {Error}", result.ErrorMessage);
                return;
            }

            // Refresh the queue
            await LoadSongsAsync();
        }

        private Task SongDeletionRequest(Song song)
        {
            pendingDeleteSong = song;
            showConfirmDelete = true;
            return Task.CompletedTask;

        }
        private async Task ConfirmDelete()
        {
            if (pendingDeleteSong is null) return;

            var result = await SongService.DeleteAsync(pendingDeleteSong).ConfigureAwait(false);

            if (!result.Success)
            {
                Logger.LogError("Failed to delete song: {Error}", result.ErrorMessage);
                showConfirmDelete = false;
                return;
            }

            // Remove from local list
            songs?.Remove(pendingDeleteSong);

            // Clear selection if needed
            if (selectedSong?.Id == pendingDeleteSong.Id)
                selectedSong = null;

            showConfirmDelete = false;
        }

        private Task CancelDelete()
        {
            showConfirmDelete = false;
            return Task.CompletedTask;
        }

        private async Task AddSongAsync(int songNumber = 0, string language = "LANGUAGE")
        {
            var newSong = new Song
            {
                Title = "Title",
                Author = "",
                LyricText = "",
                Sequence = "",
                Tags = "",
                Key = "",
                Number = songNumber,
                Language = language
            };

            // Try to save to database
            var result = await SongService.AddAsync(newSong).ConfigureAwait(false);
            if (!result.Success)
            {
                Logger.LogError("Failed to add song: {Error}", result.ErrorMessage);
                return;
            }

            // Use the saved version returned by the DB
            var createdSong = result.Data;

            if (createdSong is null)
            {
                Logger.LogError("CreateAsync returned null Data for the new song.");
                return;
            }

            // Add to the list shown in UI
            songs.Add(createdSong);

            // Select & enter edit mode so the user can immediately edit it
            selectedSong = createdSong;
            backUpSong = selectedSong.Clone();
            isEditMode = true;

            UpdateLyrics();

            await InvokeAsync(StateHasChanged);
        }
        private async Task AddSongClick()
        {
            await AddSongAsync();
        }
        private async Task AddLanguageClick()
        {
            if (selectedSong is null) return;
            // Example: Copy number, specify new language
            await AddSongAsync(selectedSong.Number, "LANGUAGE");
        }
    }
}